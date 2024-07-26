using Microsoft.Extensions.Configuration;
using Moq;
using NexusAzureFunctions.Services;
using Newtonsoft.Json;
using NexusAzureFunctions.Helpers;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using NexusAzureFunctionsTests.Helpers;
namespace NexusAzureFunctionsTests.ServicesTests;

public class NexusNotificationServiceIntegrationTests
{
    private readonly IConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;
    private readonly bool _inlcudeEmails;

    public NexusNotificationServiceIntegrationTests()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.test.settings.json")
            .Build();

        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole())!;

        _inlcudeEmails = _config.GetValue<bool>("TestFlags:EnableEmailTests");
    }

    [Fact]
    public async Task ProcessServiceBusMessagesAsync_SendsNotification_NoErrors()
    {
        if (!_inlcudeEmails)
        {
            return;
        }
        // Arrange
        var mockServiceBusMessage =  CreateServiceBusMessage(1);
        var mockMessageActions = new Mock<ServiceBusMessageActions>();
        var mockLogger = new Mock<ILogger<NexusNotificationService>>();
        var notificationService = CreateNewNotificationService(mockLogger);

        // Set up the mock message actions
        mockMessageActions.Setup(m => m.CompleteMessageAsync(mockServiceBusMessage, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await notificationService.ProcessMessageAsync(mockServiceBusMessage, mockMessageActions.Object);

        // Assert
        // No Error Logs
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never,
            "Expected no errors to be logged but an error log was found.");
    }

    [Fact]
    public async Task ProcessServiceBusMessagesAsync_SendsALotNotifications_NoErrors()
    {
        if (!_inlcudeEmails)
        {
            return;
        }
        // Arrange
        var mockServiceBusMessage =  CreateServiceBusMessage(7);
        var mockMessageActions = new Mock<ServiceBusMessageActions>();
        var mockLogger = new Mock<ILogger<NexusNotificationService>>();
        var notificationService = CreateNewNotificationService(mockLogger);

        // Set up the mock message actions
        mockMessageActions.Setup(m => m.CompleteMessageAsync(mockServiceBusMessage, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await notificationService.ProcessMessageAsync(mockServiceBusMessage, mockMessageActions.Object);

        // Assert
        // No Error Logs
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never,
            "Expected no errors to be logged but an error log was found.");
    }


    #region Helper Methods

    // Create a Service Bus message for processing
    private static ServiceBusReceivedMessage CreateServiceBusMessage(int scenerioId)
    {
        var startDate = DateTime.Today.AddDays(1);
        var endDate = DateTime.Today.AddDays(80);
        int locationId = 1234;
        // Set the message body
        var appointments = AppointmentsCreator.CreateAppointmentsList(scenerioId, startDate, endDate, locationId);
        return CreateServiceBusMessage(JsonConvert.SerializeObject(appointments));
    }

    // Create a Service Bus message for testing
    public static ServiceBusReceivedMessage CreateServiceBusMessage(string body)
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(body),
            messageId: Guid.NewGuid().ToString(),
            partitionKey: null,
            viaPartitionKey: null,
            sessionId: null,
            replyToSessionId: null,
            correlationId: null,
            subject: null,
            to: null,
            contentType: null,
            replyTo: null,
            timeToLive: TimeSpan.FromMinutes(2),
            scheduledEnqueueTime: DateTimeOffset.UtcNow,
            lockTokenGuid: Guid.NewGuid(),
            deliveryCount: 1,
            enqueuedSequenceNumber: 1,
            enqueuedTime: DateTimeOffset.UtcNow,
            deadLetterSource: null,
            sequenceNumber: 1
        );

        return message;
    }

    // Create a new instance of NexusAppointmentService
    private NexusNotificationService CreateNewNotificationService(Mock<ILogger<NexusNotificationService>> mockLogger)
    {
        var tracer = new Tracer();
        var nexusDB = new NexusDB(_config);
        var emailSender = new EmailSender(_config);
        return new NexusNotificationService(mockLogger.Object, tracer, _config, nexusDB, emailSender);
    }

    #endregion
}
