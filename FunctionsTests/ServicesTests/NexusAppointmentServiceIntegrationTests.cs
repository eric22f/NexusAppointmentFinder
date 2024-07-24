using System.Net;
using Microsoft.Extensions.Configuration;
using Moq;
using NexusAzureFunctions.Services;
using NexusAzureFunctionsTests.Models;
using Newtonsoft.Json;
using NexusAzureFunctions.Helpers;
using Microsoft.Extensions.Logging;
using Moq.Protected;

namespace NexusAzureFunctionsTests.ServicesTests;
public class NexusAppointmentServiceIntegrationTests
{
    private readonly IConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;

    public NexusAppointmentServiceIntegrationTests()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.test.settings.json")
            .Build();

        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    }

    [Fact]
    public async Task GetAppointmentAsync_ReturnsAppointment()
    {
        // Arrange
        var expectedAppointment = CreateHttpAppointmentsList(1);

        var httpClientMock = GetMockHttpClient(expectedAppointment);

        var appointmentService = CreateNewAppointmentService(httpClientMock);

        // Act
        var result = await appointmentService.ProcessAppointments();

        // Assert
        Assert.True(appointmentService.IsProcessAppointmentsSuccess);
        Assert.Equal(result.Count, expectedAppointment.Count);
        Assert.Equal(expectedAppointment[0].active, result[0].Openings);
        Assert.Equal(expectedAppointment[0].timestamp, result[0].Date.ToString("yyyy-MM-ddTHH:mm"));
    }

    #region Helper Methods

    // Create a new instance of NexusAppointmentService
    private NexusAppointmentService CreateNewAppointmentService(HttpClient client)
    {
        var tracer = new Tracer();
        var cacheFactory = new AppointmentCacheFactory(_loggerFactory, tracer, _config);
        var logger = _loggerFactory.CreateLogger<NexusAppointmentService>();
        return new NexusAppointmentService(logger, _config, tracer, cacheFactory, client);
    }

    private static List<HttpAppointment> CreateHttpAppointmentsList(int scenerioId)
    {
        List<HttpAppointment> appointments = [];
        switch (scenerioId) 
        {
            case 0:
                // No appointments
                break;
            case 1:
                // Single appointment
                appointments.Add(new HttpAppointment { active = 1, total = 3, pending = 0, conflicts = 0, duration = 10, timestamp = DateTime.Now.ToString(), remote = false });
                break;
            default:
                throw new Exception("Invalid scenerioId");
        }
        return appointments;
    }

    private static HttpClient GetMockHttpClient(List<HttpAppointment> expectedAppointment)
    {
        var httpClientMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        
        httpClientMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(expectedAppointment)),
            })
            .Verifiable();

        var httpClient = new HttpClient(httpClientMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com/"),
        };

        return httpClient;
    }
#endregion
}