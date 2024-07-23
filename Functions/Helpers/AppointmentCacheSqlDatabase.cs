using System.Data.SqlClient;
using System.Data;
using NexusAzureFunctions.Models;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace NexusAzureFunctions.Helpers;

public class AppointmentCacheSqlDatabase : AppointmentCacheBase
{
    private readonly string _connectionString;

    public AppointmentCacheSqlDatabase(IConfiguration configuration)
    {
        var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connectionString = config["SqlDatabase:SqlConnectionString"] ?? 
            throw new ConfigurationErrorsException("Configuration setting 'SqlDatabase:SqlConnectionString' not found.");
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
        int retry = 1;

        while (retry >= 0)
        {
            try
            {
                using SqlConnection connection = new(_connectionString);
                string query = "SELECT * FROM NexusAppointmentsAvailability WHERE LocationId = @LocationId"
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
                        Date = reader.GetDateTime(1),
                        LocationId = reader.GetInt32(2),
                        Openings = reader.GetInt16(3),
                        TotalSlots = reader.GetInt16(4),
                        Pending = reader.GetInt16(5),
                        Conflicts = reader.GetInt16(6),
                        Duration = reader.GetInt16(7)
                    });
                }
                // Done
                break;
            }
            catch (TimeoutException) when (retry-- > 0)
            {
                continue;
            }
        }

        return appointments;
    }
    
    #region Private Methods

    // Clear any existing appointments for the location by date range
    private void ClearAppointmentsByDate(int locationId, DateTime startDate, DateTime endDate)
    {
        int retry = 1;

        while (retry >= 0)
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
            catch (TimeoutException) when (retry-- > 0)
            {
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