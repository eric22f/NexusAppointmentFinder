using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexusAzureFunctions.Helpers;
using NexusAzureFunctions.Models;
using NexusAzureFunctionsTests.Helpers;
using Xunit.Abstractions;

namespace NexusAzureFunctionsTests.HelpersTests;

public class AppointmentCacheSqlDatabaseIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<AppointmentCacheSqlDatabase> _logger;
    private readonly IConfiguration _config;
    private readonly string _connectionString;
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;
    private readonly int _locationId = 1234;
    private const int MaxScenerioId = 7;

    public AppointmentCacheSqlDatabaseIntegrationTests(ITestOutputHelper output)
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
        _logger = loggerFactory.CreateLogger<AppointmentCacheSqlDatabase>();

        _connectionString = _config["SqlDatabase:SqlConnectionString"] ?? throw new ConfigurationErrorsException("Configuration setting 'SqlDatabase:SqlConnectionString' not found.");
        int totalDays = _config.GetValue<int>("NexusApi:TotalDays");
        if (totalDays < 1)
        {
            totalDays = 7;
        }
        _startDate = DateTime.Today.AddDays(1);
        _endDate = _startDate.AddDays(totalDays);
    }

    [Fact]
    public void SqlCache_CacheAppointments_SavesExpectedAppointments()
    {
        _output.WriteLine("Running Test SqlCache_CacheAppointments_SavesExpectedAppointments");
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

    private AppointmentCacheSqlDatabase CreateAppointmentCache()
    {
        return new AppointmentCacheSqlDatabase(_config, _logger, new Tracer());
    }

    private void ClearCache()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM NexusAppointmentsAvailability";
        command.CommandType = CommandType.Text;
        command.ExecuteNonQuery();
        // Reset the identity seed
        command.CommandText = "DBCC CHECKIDENT ('NexusAppointmentsAvailability', RESEED, 0)";
        command.ExecuteNonQuery();
    }

#endregion
}
