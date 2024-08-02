
using Microsoft.Extensions.Logging;
using Moq;

namespace NexusAzureFunctionsTests.Helpers;

public class MockLoggerCreator
{
    // Create a mock logger that also writes to the console
    public static Mock<ILogger<T>> Create<T>()
    {
        var mockLogger = new Mock<ILogger<T>>();
        mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
        )).Callback((LogLevel logLevel, EventId eventId, object state, Exception exception, Delegate formatter) =>
        {
            var logMessage = formatter.DynamicInvoke(state, exception) as string;
            Console.WriteLine($"[{logLevel}] {logMessage}");
        });
        return mockLogger;
    }
}