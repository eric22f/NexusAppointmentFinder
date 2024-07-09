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
        public static AppointmentCacheBase CreateCacheClient(IConfiguration configuration)
        {
            // Create new Redis cache client
            try
            {
                var cache = new AppointmentCacheRedis();
                return cache;
            }
            catch (Exception)
            {
                // Log the exception
                var logger = new LoggerFactory().CreateLogger<AppointmentCacheBase>();
                logger.LogError("Unable to create Redis cache client");
            }
            try
            {
                // Create new Database cache client
                return new AppointmentCacheSqlDatabase();
            }
            catch (Exception)
            {
                // Log the exception
                var logger = new LoggerFactory().CreateLogger<AppointmentCacheBase>();
                logger.LogError("Unable to create Database cache client");
            }
            throw new Exception("Unable to create cache client");
        }
    }
}