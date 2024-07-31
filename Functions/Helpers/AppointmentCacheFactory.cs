using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexusAzureFunctions.Helpers;

// This cache factory is used to create an enabled and valid cache client in this order Redis, SQL Database, then Blob Storage
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
            if (_config["SqlDatabase:Enabled"] == "true")
            {
                // Create new Database cache client
                return new AppointmentCacheSqlDatabase(_config, _loggerFactory.CreateLogger<AppointmentCacheSqlDatabase>(), tracer);
            }
        }
        catch (Exception e)
        {
            // Log the exception
            _loggerFactory.CreateLogger<AppointmentCacheBase>().LogError(e, $"[{_traceId}] Unable to create Database cache client.");
        }
        try
        {
            return new AppointmentCacheBlobStorage(_config, _loggerFactory.CreateLogger<AppointmentCacheBlobStorage>(), tracer);
        }
        catch (Exception e)
        {
            // Log the exception
            _loggerFactory.CreateLogger<AppointmentCacheBase>().LogError(e, $"[{_traceId}] Unable to create Blob storage cache client.");
        }
        throw new Exception("Unable to create cache client");
    }
}