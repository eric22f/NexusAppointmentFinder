using System.Data.SqlClient;
using System.Data;
using NexusAzureFunctions.Models;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Microsoft.Extensions.Logging;

namespace NexusAzureFunctions.Helpers;

public class AppointmentCacheSqlDatabase : AppointmentCacheBase
{
    private readonly string _connectionString;
    private readonly ILogger<AppointmentCacheSqlDatabase> _logger;
    private readonly Tracer _tracer;

    public AppointmentCacheSqlDatabase(IConfiguration configuration, ILogger<AppointmentCacheSqlDatabase> logger,
        Tracer tracer)
    {
        var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connectionString = config["SqlDatabase:SqlConnectionString"] ?? 
            throw new ConfigurationErrorsException("Configuration setting 'SqlDatabase:SqlConnectionString' not found.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    // Add the appointment to the database
    public override void CacheAppointments(int locationId, DateTime startDate, DateTime endDate, List<Appointment> appointments)
    {
        ClearAppointmentsByDate(locationId, startDate, endDate);

        // Insert the open appointments
        InsertAppointments(appointments);
    }
    
    // Get the appointments from the database by location and date range
    protected override List<Appointment> GetCachedAppointments(int locationId, DateTime startDate, DateTime endDate)
    {
        List<Appointment> appointments = [];
        int retry = 3;
        int retryCount = 0;

        while (retryCount < retry)
        {
            try
            {
                using SqlConnection connection = new(_connectionString);
                string query = "SELECT AppointmentDate, LocationId, Openings, TotalSlots ,Pending"
                             + ", Conflicts, Duration"
                             + " FROM NexusAppointmentsAvailability WHERE LocationId = @LocationId"
                             + " AND AppointmentDate >= @StartDate AND AppointmentDate <= @EndDate";
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@LocationId", locationId);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                connection.Open();
                using SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    appointments.Add(new Appointment
                    {
                        Date = (DateTime)reader["AppointmentDate"],
                        LocationId = Convert.ToInt32(reader["LocationId"]),
                        Openings = Convert.ToInt32(reader["Openings"]),
                        TotalSlots = Convert.ToInt32(reader["TotalSlots"]),
                        Pending = Convert.ToInt32(reader["Pending"]),
                        Conflicts = Convert.ToInt32(reader["Conflicts"]),
                        Duration = Convert.ToInt32(reader["Duration"])
                    });
                }
                // Done
                break;
            }
            catch (SqlException ex) when (ex.Number == -2 && retryCount++ < retry)
            {
                _logger.LogWarning($"[{_tracer.Id}] Timeout exception occurred. Retry attempt {retryCount} of {retry}...");
                Thread.Sleep(2000);
                continue;
            }
            catch (InvalidOperationException) when (retryCount++ < retry)
            {
                _logger.LogWarning($"[{_tracer.Id}] Invalid Operation exception occurred. Retry attempt {retryCount} of {retry}...");
                Thread.Sleep(2000);
                continue;
            }
        }

        return appointments;
    }
    
    #region Private Methods

    // Clear any existing appointments for the location by date range
    private void ClearAppointmentsByDate(int locationId, DateTime startDate, DateTime endDate)
    {
        int retry = 3;
        int retryCount = 0;

        while (retryCount < retry)
        {
            try
            {
                using SqlConnection connection = new(_connectionString);
                // Clear out any existing appointments for the location by date range
                bool includeStartDate = startDate.AddDays(-1) > DateTime.Today;
                string query = "DELETE FROM NexusAppointmentsAvailability"
                             + " WHERE LocationId = @LocationId"
                             + (includeStartDate ? " AND AppointmentDate >= @StartDate" : "")
                             + " AND AppointmentDate <= @EndDate";
                SqlCommand command = new(query, connection);
                command.Parameters.AddWithValue("@LocationId", locationId);
                if (includeStartDate)
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                }
                command.Parameters.AddWithValue("@EndDate", endDate);
                connection.Open();
                command.ExecuteNonQuery();
                // Done
                break;
            }
            catch (SqlException ex) when (ex.Number == -2 && retryCount++ < retry)
            {
                _logger.LogWarning($"[{_tracer.Id}] Timeout exception occurred. Retry attempt {retryCount} of {retry}...");
                Thread.Sleep(2000);
                continue;
            }
            catch (InvalidOperationException) when (retryCount++ < retry)
            {
                _logger.LogWarning($"[{_tracer.Id}] Invalid Operation exception occurred. Retry attempt {retryCount} of {retry}...");
                Thread.Sleep(2000);
                continue;
            }
        }
    }

    // Insert the appointment into the database
    private void InsertAppointments(List<Appointment> appointments)
    {
        if (appointments.Count == 0)
        {
            return;
        }
        using var copy = new SqlBulkCopy(_connectionString);
        copy.DestinationTableName = "NexusAppointmentsAvailability";

        // Ensure that the column mappings match the DataTable and the SQL table schema
        copy.ColumnMappings.Add("AppointmentDate", "AppointmentDate");
        copy.ColumnMappings.Add("LocationId", "LocationId");
        copy.ColumnMappings.Add("Openings", "Openings");
        copy.ColumnMappings.Add("TotalSlots", "TotalSlots");
        copy.ColumnMappings.Add("Pending", "Pending");
        copy.ColumnMappings.Add("Conflicts", "Conflicts");
        copy.ColumnMappings.Add("Duration", "Duration");
        
        copy.WriteToServer(ToDataTable(appointments));
    }
    
    private static DataTable ToDataTable(List<Appointment> appointments)
    {
        DataTable dataTable = new();
        // Add columns to the DataTable
        dataTable.Columns.Add("AppointmentDate", typeof(DateTime));
        dataTable.Columns.Add("LocationId", typeof(int));
        dataTable.Columns.Add("Openings", typeof(short));
        dataTable.Columns.Add("TotalSlots", typeof(short));
        dataTable.Columns.Add("Pending", typeof(short));
        dataTable.Columns.Add("Conflicts", typeof(short));
        dataTable.Columns.Add("Duration", typeof(short));
    
        // Add rows to the DataTable
        foreach (var appointment in appointments)
        {
            dataTable.Rows.Add(
                appointment.Date,
                appointment.LocationId,
                appointment.Openings,
                appointment.TotalSlots,
                appointment.Pending,
                appointment.Conflicts,
                appointment.Duration
            );
        }
    
        return dataTable;
    }
    #endregion
}