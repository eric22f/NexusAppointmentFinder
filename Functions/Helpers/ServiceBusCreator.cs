using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace NexusAzureFunctions.Helpers;

public static class ServiceBusCreator
{
    public static QueueClient CreateServiceBusClient(IConfiguration configuration, string connectionString = "", string queueName = "")
    {
        connectionString = string.IsNullOrWhiteSpace(connectionString) ?
            configuration["ServiceBusConnectionString"] + "" : connectionString;
        if (string.IsNullOrWhiteSpace(connectionString)){
            connectionString = configuration["Values:ServiceBusConnectionString"] + "";
        }

        queueName = string.IsNullOrWhiteSpace(queueName) ?
            configuration["ServiceBusQueueName"] + "" : queueName;
        if (string.IsNullOrWhiteSpace(queueName)) {
            queueName = configuration["Values:ServiceBusQueueName"] + "";
        }

        return new QueueClient(connectionString, queueName);
    }
}