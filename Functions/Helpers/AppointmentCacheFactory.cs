using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexusAzureFunctions.Helpers;

// This static class is used to create a cache client by first connecting to Redis
// If Redis is not available then to a database
public class AppointmentCacheFactory(ILoggerFactory loggerFactory, Tracer tracer, IConfiguration config)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly string _traceId = tracer.Id ?? throw new ArgumentNullException(nameof(tracer.Id));
    private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    public AppointmentCacheBase CreateCacheClient()
    {
        // Create new Redis cache client
        try
        {
            if (_config["RedisCache:Enabled"] == "true")
            {
                var cache = new AppointmentCacheRedis(_loggerFactory.CreateLogger<AppointmentCacheRedis>(), _traceId, _config);
                return cache;
            }
        }
        catch (Exception e)
        {
            // Log the exception
            _loggerFactory.CreateLogger<AppointmentCacheBase>().LogError(e, $"[{_traceId}] Unable to create Redis cache client.");
        }
        try
        {
            // Create new Database cache client
            return new AppointmentCacheSqlDatabase(_loggerFactory.CreateLogger<AppointmentCacheSqlDatabase>(), _traceId, _config);
        }
        catch (Exception e)
        {
            // Log the exception
            _loggerFactory.CreateLogger<AppointmentCacheBase>().LogError(e, $"[{_traceId}] Unable to create Database cache client.");
        }
        throw new Exception("Unable to create cache client");
    }
}