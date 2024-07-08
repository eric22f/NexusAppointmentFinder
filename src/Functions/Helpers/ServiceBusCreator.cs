using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Functions.Helpers
{
    public static class ServiceBusCreator
    {
        public static QueueClient CreateServiceBusClient(IConfiguration configuration, string connectionString = "", string queueName = "")
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(queueName))
            {
                connectionString = string.IsNullOrWhiteSpace(connectionString) ?
                    configuration["ServiceBusConnectionString"] ?? "ServiceBusConnectionString - not found" : connectionString;
                queueName = string.IsNullOrWhiteSpace(queueName) ?
                    configuration["QueueName"] ?? "QueueName - not found" : queueName;
            }

            return new QueueClient(connectionString, queueName);
        }
    }
}