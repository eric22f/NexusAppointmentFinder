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
    // Test the NexusAppointmentService.ProcessAppointments method
    // This will create one new service bus messages if enabled in local.test.settings.json
    public async Task GetAppointmentAsync_ReceivesSingleAppointment_SubmitsAppointment()
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

    [Fact]
    // Test the NexusAppointmentService.ProcessAppointments method
    // This will create new 300+ service bus messages if enabled in local.test.settings.json
    public async Task GetAppointmentAsync_ReceivesAppointments_SubmitsAppointments()
    {
        // Arrange
        var expectedAppointment = CreateHttpAppointmentsList(3);

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
            case 2:
                // Multiple appointments
                appointments.Add(new HttpAppointment { active = 1, total = 3, pending = 0, conflicts = 0, duration = 10, timestamp = DateTime.Now.AddDays(1).ToString(), remote = false });
                appointments.Add(new HttpAppointment { active = 2, total = 3, pending = 0, conflicts = 0, duration = 10, timestamp = DateTime.Now.AddDays(2).ToString(), remote = false });
                appointments.Add(new HttpAppointment { active = 3, total = 3, pending = 0, conflicts = 0, duration = 10, timestamp = DateTime.Now.AddDays(4).ToString(), remote = false });
                appointments.Add(new HttpAppointment { active = 1, total = 3, pending = 0, conflicts = 0, duration = 10, timestamp = DateTime.Now.AddDays(7).ToString(), remote = false });
                break;
            case 3:
                // For 180 days every day has a consecutive appointment every 10 minutes with an opening starting 8 am to 6 pm
                var firstAppointmentDateTime = DateTime.Today.AddDays(1).AddHours(8);
                for (int i = 0; i < 180; i++)
                {
                    var appointmentDateTime = firstAppointmentDateTime.AddDays(i);
                    while (appointmentDateTime.Hour < 18)
                    {
                        appointments.Add(new HttpAppointment { active = 1, total = 3, pending = 0, conflicts = 0, duration = 10, timestamp = appointmentDateTime.ToString(), remote = false });
                        appointmentDateTime = appointmentDateTime.AddMinutes(10);
                    }
                }
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