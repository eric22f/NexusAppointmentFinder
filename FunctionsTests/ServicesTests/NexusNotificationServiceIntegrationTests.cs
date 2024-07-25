using System.Net;
using Microsoft.Extensions.Configuration;
using Moq;
using NexusAzureFunctions.Services;
using NexusAzureFunctionsTests.Models;
using Newtonsoft.Json;
using NexusAzureFunctions.Helpers;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using System.Text;
using NexusAzureFunctionsTests.Helpers;

namespace NexusAzureFunctionsTests.ServicesTests;

public class NexusNotificationServiceIntegrationTests
{
    private readonly IConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;

    public NexusNotificationServiceIntegrationTests()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.test.settings.json")
            .Build();

        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    }

//    [Fact]
//    public async Task ProcessServiceBusMessagesAsync_SendsNotification()
//    {

//    }


    #region Helper Methods

    // Create a Service Bus message for processing
    private ServiceBusReceivedMessage CreateServiceBusMessage(int scenerioId)
    {
        // Create a mock Service Bus message
        var message = new Mock<ServiceBusReceivedMessage>();
        
        // Set the message body
        var appointments = AppointmentsCreator.CreateAppointmentsList(scenerioId, DateTime.Today, DateTime.Today.AddDays(7), 1234);
        var messageBodyJson = JsonConvert.SerializeObject(appointments);
        message.Setup(m => m.Body).Returns(new BinaryData(Encoding.UTF8.GetBytes(messageBodyJson)));
        
        return message.Object;
    }

    // Create a new instance of NexusAppointmentService
    private NexusNotificationService CreateNewNotificationService()
    {
        var tracer = new Tracer();
        var logger = _loggerFactory.CreateLogger<NexusNotificationService>();
        var nexusDB = new NexusDB(_config);
        return new NexusNotificationService(logger, tracer, _config, nexusDB);
    }

    #endregion
}
