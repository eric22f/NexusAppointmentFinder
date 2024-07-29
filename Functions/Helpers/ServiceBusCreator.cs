using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace NexusAzureFunctions.Helpers;

public static class ServiceBusCreator
{
    public static QueueClient CreateServiceBusClient(IConfiguration configuration, string connectionString = "", string queueName = "")
    {
        connectionString = string.IsNullOrWhiteSpace(connectionString) ?
            configuration["ServiceBusConnectionString"] + "" : connectionString;

        queueName = string.IsNullOrWhiteSpace(queueName) ?
            configuration["ServiceBusQueueName"] + "" : queueName;

        return new QueueClient(connectionString, queueName);
    }
}