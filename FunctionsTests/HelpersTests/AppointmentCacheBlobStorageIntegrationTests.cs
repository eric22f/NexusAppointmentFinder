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
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;
    private readonly int _locationId = 1234;
    private const int MaxScenerioId = 7;
    private readonly bool _runBlobStorageTests;

    public AppointmentCacheBlobStorageIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("local.test.settings.json", optional: true, reloadOnChange: true);
        _config = configurationBuilder.Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(_config.GetSection("Logging"))
                .AddConsole();
        });
        _logger = loggerFactory.CreateLogger<AppointmentCacheBlobStorage>();

        int totalDays = 180;
        _startDate = DateTime.Today.AddDays(1);
        _endDate = _startDate.AddDays(totalDays);
        _runBlobStorageTests = _config["TestFlags:EnableBlobStorageTests"] == "true";
    }

    [Fact]
    public void BlobCache_CacheAppointments_SavesExpectedAppointments()
    {
        if (!_runBlobStorageTests)
        {
            _output.WriteLine("Skipping test. Enable Blob Storage tests in local.test.settings.json to run this test.");
            return;
        }
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
            // Clean up
            cache.ClearCache(_locationId);
        }
    }


#region Private Functions

    private AppointmentCacheBlobStorage CreateAppointmentCache()
    {
        return new AppointmentCacheBlobStorage(_config, _logger, new Tracer());
    }

#endregion
}
