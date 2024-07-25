using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NexusAzureFunctions.Models;

namespace NexusAzureFunctions.Helpers;

public class NexusDB
{
    // Replace this with your actual data access logic
    private readonly string _connectionString;

    public NexusDB(IConfiguration configuration)
    {
        var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connectionString = config["SqlDatabase:SqlConnectionString"] ?? 
            throw new ConfigurationErrorsException("Configuration setting 'SqlDatabase:SqlConnectionString' not found.");
    }
    
    // Get all users that have registered for this location
    public List<UserNotifications> GetUsersAssignedToLocation(int locationId)
    {
        List<UserNotifications> users = [];

        // Replace this with your actual query to retrieve users
        using SqlConnection connection = new(_connectionString);
        string query = "SELECT u.UserId, u.Email, u.AlternateEmail, u.Phone, u.PhoneProviderId, u.FirstName, u.LastName,"
                     + " u.NotifyByEmail, u.NotifyBySms,"
                     + " u.EmailConfirmed, u.AlternateEmailConfirmed, u.PhoneConfirmed,"
                     + " l.LocationId, l.LocationName, l.LocationDescription"
                     + " FROM NexusUsers u"
                     + " INNER JOIN NexusUserLocations ul ON u.UserId = ul.UserId"
                     + " INNER JOIN NexusLocations l ON ul.LocationId = l.LocationId"
                     + " WHERE l.LocationId = @LocationId"
                     + " AND u.IsActive = 1 AND l.IsActive = 1";
        SqlCommand command = new(query, connection);
        command.Parameters.AddWithValue("@LocationId", locationId);

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new UserNotifications
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                Email = reader["Email"]?.ToString() ?? string.Empty,
                AlternateEmail = reader["AlternateEmail"]?.ToString() ?? string.Empty,
                Phone = reader["Phone"]?.ToString() ?? string.Empty,
                PhoneProviderId = Convert.ToInt32(reader["PhoneProviderId"]),
                FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                LastName = reader["LastName"]?.ToString() ?? string.Empty,
                NotifyByEmail = Convert.ToBoolean(reader["NotifyByEmail"]),
                NotifyBySms = Convert.ToBoolean(reader["NotifyBySms"]),
                EmailConfirmed = Convert.ToBoolean(reader["EmailConfirmed"]),
                AlternateEmailConfirmed = Convert.ToBoolean(reader["AlternateEmailConfirmed"]),
                PhoneConfirmed = Convert.ToBoolean(reader["PhoneConfirmed"]),
                LocationId = Convert.ToInt32(reader["LocationId"]),
                LocationName = reader["LocationName"]?.ToString() ?? string.Empty,
                LocationDescription = reader["LocationDescription"].ToString()
            });
        }

        return users;
    }
}
