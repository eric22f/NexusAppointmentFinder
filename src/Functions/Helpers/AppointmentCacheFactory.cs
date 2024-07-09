using Functions.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Functions.Helpers
{
    // This static class is used to create a cache client by first connecting to Redis
    // If Redis is not available then to a database
    public static class AppointmentCacheFactory
    {
        public static AppointmentCacheBase CreateCacheClient(IConfiguration configuration, ILogger<AppointmentCacheBase> logger, string traceId)
        {
            // Create new Redis cache client
            try
            {
                var cache = new AppointmentCacheRedis((ILogger<AppointmentCacheRedis>)logger, traceId, configuration);
                return cache;
            }
            catch (Exception e)
            {
                // Log the exception
                logger.LogError($"[{traceId}]Unable to create Redis cache client: {e.Message}", e);
            }
            try
            {
                // Create new Database cache client
                return new AppointmentCacheSqlDatabase((ILogger<AppointmentCacheSqlDatabase>)logger, traceId, configuration);
            }
            catch (Exception e)
            {
                // Log the exception
                logger.LogError($"[{traceId}]Unable to create Database cache client: {e.Message}", e);
            }
            throw new Exception("Unable to create cache client");
        }
    }
}