using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NexusAzureFunctions.Helpers;
using NexusAzureFunctions.Models;
using Xunit;

namespace NexusAzureFunctions.Tests.Helpers;

public class AppointmentCacheSqlDatabaseIntegrationTests
{
    private readonly ILogger<AppointmentCacheSqlDatabase> _logger;
    private readonly IConfiguration _config;
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;
    private readonly int _locationId = 1234;
    private const int MaxScenerioId = 7;

    public AppointmentCacheSqlDatabaseIntegrationTests()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        _config = configurationBuilder.Build();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(_config.GetSection("Logging"))
                    .AddConsole();
        });
        _logger = loggerFactory.CreateLogger<AppointmentCacheSqlDatabase>();

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
        _logger.LogInformation("Running SqlCache_CacheAppointments_SavesExpectedAppointments");
        _logger.LogInformation($"Total Scenerios: {MaxScenerioId + 1}");
        // Go through each Scenerio
        for (int scenerioId = 0; scenerioId <= MaxScenerioId; scenerioId++)
        {
            _logger.LogInformation($"Scenerio: {scenerioId}");
            // Arrange
            var cache = CreateAppointmentCache();
            var appointments = CreateAppointmentsList(scenerioId);

            // Act
            cache.CacheAppointments(_locationId, _startDate, _endDate, appointments);

            // Assert
            var cachedAppointments = cache.GetCachedAppointments(_locationId, _startDate, _endDate);
            Assert.Equal(appointments.Count, cachedAppointments.Count);
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
            _logger.LogInformation($"Scenerio: {scenerioId} - Passed");
        }
    }


#region Private Functions

    private AppointmentCacheSqlDatabase CreateAppointmentCache()
    {
        return new AppointmentCacheSqlDatabase(_logger, new Tracer().Id, _config);
    }

    private List<Appointment> CreateAppointmentsList(int scenerioId)
    {
        var result = new List<Appointment>();
        var startDateTime = _startDate.AddHours(8);

        switch (scenerioId)
        {
            case 0:
                // Empty list
                break;
            case 1:
                // One appointment on the first day
                result.Add(new Appointment { Date = startDateTime, LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 2:
                // One appointment on the 5th day
                result.Add(new Appointment { Date = startDateTime.AddDays(5), LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 3:
                // One appointment on the last day
                result.Add(new Appointment { Date = _endDate.AddHours(17), LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 4:
                // Multiple appointments
                result.Add(new Appointment { Date = startDateTime, LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                result.Add(new Appointment { Date = startDateTime.AddDays(1), LocationId = _locationId, Openings = 2, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                result.Add(new Appointment { Date = startDateTime.AddDays(2), LocationId = _locationId, Openings = 3, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                result.Add(new Appointment { Date = _endDate.AddHours(12), LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 5:
                // A single appointment every day
                for (int i = 0; i < _endDate.Subtract(_startDate).Days; i++)
                {
                    result.Add(new Appointment { Date = startDateTime.AddDays(i), LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                }
                break;
            case 6:
                // Every other day has an appointment
                for (int i = 0; i < _endDate.Subtract(_startDate).Days; i++)
                {
                    if (i % 2 == 0)
                    {
                        result.Add(new Appointment { Date = startDateTime.AddDays(i), LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                    }
                }
                break;
            case 7:
                // Every day has a consecutive appointment every 10 minutes with an opening starting 8 am to 6 pm
                for (int i = 0; i < _endDate.Subtract(_startDate).Days; i++)
                {
                    startDateTime = startDateTime.AddDays(i).AddHours(8);
                    while (startDateTime.Hour < 18)
                    {
                        result.Add(new Appointment { Date = startDateTime, LocationId = _locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                        startDateTime = startDateTime.AddMinutes(10);
                    }
                }
                break;
            default:
                throw new ArgumentException("Invalid scenerioId");
        }

        return result;
    }

    private void ClearCache()
    {
        using var connection = new SqlConnection(_config.GetConnectionString("SqlConnectionString"));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Appointments";
        command.CommandType = CommandType.Text;
        command.ExecuteNonQuery();
    }

    private void SeedCache()
    {
        using var connection = new SqlConnection(_config.GetConnectionString("SqlConnectionString"));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Appointments (Id, StartTime, EndTime, Title, Description) VALUES ('1', '2021-01-01 00:00:00', '2021-01-01 01:00:00', 'Test', 'Test')";
        command.CommandType = CommandType.Text;
        command.ExecuteNonQuery();
    }

#endregion
}
