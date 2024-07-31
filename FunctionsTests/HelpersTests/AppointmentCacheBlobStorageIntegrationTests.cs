using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexusAzureFunctions.Helpers;
using NexusAzureFunctions.Models;
using NexusAzureFunctionsTests.Helpers;
using Xunit.Abstractions;

namespace NexusAzureFunctionsTests.HelpersTests;

public class AppointmentCacheBlobStorageIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<AppointmentCacheBlobStorage> _logger;
    private readonly IConfiguration _config;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly string _blobStorageName;
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;
    private readonly int _locationId = 1234;
    private const int MaxScenerioId = 7;

    public AppointmentCacheBlobStorageIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.test.settings.json", optional: true, reloadOnChange: true);
        _config = configurationBuilder.Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(_config.GetSection("Logging"))
                .AddConsole();
        });
        _logger = loggerFactory.CreateLogger<AppointmentCacheBlobStorage>();

        string blobConnectionsString = _config["BlobStorage:BlobStorageConnectionString"] ?? 
            throw new ConfigurationErrorsException("Configuration setting 'BlobStorage:BlobStorageConnectionString' not found.");
        _blobServiceClient = new BlobServiceClient(blobConnectionsString);
        string blobContainerName = _config["BlobStorage:ContainerName"] ?? 
            throw new ConfigurationErrorsException("Configuration setting 'BlobStorage:ContainerName' not found.");
        _blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainerName);
        _blobStorageName = $"Appointments_LocationId_{_locationId}.json";
        int totalDays = _config.GetValue<int>("NexusApi:TotalDays");
        if (totalDays < 1)
        {
            totalDays = 7;
        }
        _startDate = DateTime.Today.AddDays(1);
        _endDate = _startDate.AddDays(totalDays);
    }

    [Fact]
    public void BlobCache_CacheAppointments_SavesExpectedAppointments()
    {
        _output.WriteLine("Running Test BlobCache_CacheAppointments_SavesExpectedAppointments");
        _output.WriteLine($"Total Scenerios: {MaxScenerioId + 1}");
        // Go through each Scenerio
        for (int scenerioId = 0; scenerioId <= MaxScenerioId; scenerioId++)
        {
            _output.WriteLine($"Scenerio: {scenerioId}");
            // Arrange
            var cache = CreateAppointmentCache();
            var appointments = AppointmentsCreator.CreateAppointmentsList(scenerioId, _startDate, _endDate, _locationId);

            // Act
            cache.CacheAppointments(_locationId, _startDate, _endDate, appointments);

            // Assert
            cache.LoadCachedAppointments(_locationId, _startDate, _endDate);
            var cachedAppointments = cache.GetCachedAppointments();
            Assert.True(appointments.Count == cachedAppointments.Count, $"Scenerio: {scenerioId} - Count mismatch");
            for (int i = 0; i < appointments.Count; i++)
            {
                Assert.Equal(appointments[i].Date, cachedAppointments[i].Date);
                Assert.Equal(appointments[i].LocationId, cachedAppointments[i].LocationId);
                Assert.Equal(appointments[i].Openings, cachedAppointments[i].Openings);
                Assert.Equal(appointments[i].TotalSlots, cachedAppointments[i].TotalSlots);
                Assert.Equal(appointments[i].Pending, cachedAppointments[i].Pending);
                Assert.Equal(appointments[i].Conflicts, cachedAppointments[i].Conflicts);
                Assert.Equal(appointments[i].Duration, cachedAppointments[i].Duration);
            }
            _output.WriteLine($"Scenerio: {scenerioId} - Passed");
        }

        // Clean up
        ClearCache();
    }


#region Private Functions

    private AppointmentCacheBlobStorage CreateAppointmentCache()
    {
        return new AppointmentCacheBlobStorage(_config, _logger, new Tracer());
    }

    // Clear the cache
    private void ClearCache()
    {
        BlobClient blobClient = _blobContainerClient.GetBlobClient(_blobStorageName);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("[]"));
        blobClient.Upload(stream, overwrite: true);
    }

#endregion
}
