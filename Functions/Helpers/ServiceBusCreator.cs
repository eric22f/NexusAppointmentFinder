using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace NexusAzureFunctions.Helpers;

public static class ServiceBusCreator
{
    public static QueueClient CreateServiceBusClient(IConfiguration configuration, string connectionString = "", string queueName = "")
    {
        connectionString = string.IsNullOrWhiteSpace(connectionString) ?
            configuration["ServiceBus:ServiceBusConnectionString"] + "" : connectionString;

        queueName = string.IsNullOrWhiteSpace(queueName) ?
            configuration["ServiceBus:QueueName"] + "" : queueName;

        return new QueueClient(connectionString, queueName);
    }
}