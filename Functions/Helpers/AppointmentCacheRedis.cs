using StackExchange.Redis;
using Newtonsoft.Json;
using NexusAzureFunctions.Models;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Microsoft.Extensions.Logging;

namespace NexusAzureFunctions.Helpers;

// Cache processed appointments in Redis
// Appointments are stored by location and date
public class AppointmentCacheRedis : AppointmentCacheBase
{
    private readonly ILogger<AppointmentCacheRedis> _logger;
    private readonly string _traceId;
    private readonly IConfiguration _config;
    private readonly IDatabase _redisDatabase;

    public AppointmentCacheRedis(ILogger<AppointmentCacheRedis> logger, string traceId, IConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _traceId = traceId ?? throw new ArgumentNullException(nameof(traceId));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        string redisConnectionString = config["RedisCache:RedisConnectionString"] ?? throw new ConfigurationErrorsException("Configuration setting 'RedisCache:RedisConnectionString' not found.");
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
        _redisDatabase = redisConnection.GetDatabase();
        // Check if the Redis database connection is valid
        if (_redisDatabase.Ping() == TimeSpan.Zero)
        {
            throw new Exception("Failed to establish connection to Redis database.");
        }
    }

    // Mark an appointment as processed
    public override void CacheAppointments(int locationId, DateTime startDate, DateTime endDate, List<Appointment> appointments)
    {
        // Update Redis cache by location and date range
        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var key = GenerateRedisCacheKey(locationId, date);
            // Get appointments for the date
            var appointmentsForDate = appointments.Where(a => a.Date.Date == date).ToList();
            // Serialize the appointments
            var serializedAppointments = JsonConvert.SerializeObject(appointmentsForDate);
            // Store the appointments in Redis by location and date.  Expire the day after "date"
            _redisDatabase.StringSet(key, serializedAppointments, expiry: date.AddDays(1) - DateTime.Now, flags: CommandFlags.FireAndForget);
        }
    }
    // Get the appointments from the cache by location and date range
    protected override List<Appointment> GetCachedAppointments(int locationId, DateTime startDate, DateTime endDate)
    {
        var appointments = new List<Appointment>();
        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var key = GenerateRedisCacheKey(locationId, date);
            // Get appointments for this date
            var redisValue = _redisDatabase.StringGet(key);
            if (redisValue.IsNullOrEmpty)
            {
                continue;
            }
            string serializedAppointments = redisValue.ToString();
            var appointmentsForDate = JsonConvert.DeserializeObject<List<Appointment>>(serializedAppointments) ?? [];
            appointments.AddRange(appointmentsForDate);
        }
        return appointments;
    }

    // Clear the cache for a location
    public override void ClearCache(int locationId)
    {
        // Clear all appointments for the location
        var keys = _redisDatabase.Multiplexer.GetServer(_redisDatabase.Multiplexer.GetEndPoints()[0]).Keys(pattern: $"{locationId}-*");
        foreach (var key in keys)
        {
            _redisDatabase.KeyDelete(key);
        }
    }

    #region Private Methods

    // Generate a unique key used to cache all open appointments for a given location on a given date
    protected static string GenerateRedisCacheKey(Appointment appointment)
    {
        return GenerateRedisCacheKey(appointment.LocationId, appointment.Date);
    }
    protected static string GenerateRedisCacheKey(int locationId, DateTime date)
    {
        return $"{locationId}-{date.ToString("yyyy-MM-dd")}";
    }
    #endregion
}