using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexusAzureFunctions.Helpers;
using NexusAzureFunctions.Services;

namespace NexusAzureFunctions;

public class SBQueueAppointmentNotificationAlerts(NexusNotificationService notificationService)
{
    private readonly NexusNotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

    [Function("TriggerNexusAppointmentNotificationAlerts")]
    public async Task Run(
        [ServiceBusTrigger("nexus-api-queue-dev", Connection = "ServiceBusConnectionString")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        // Process the message
        await _notificationService.ProcessMessageAsync(message, messageActions);
    }
}