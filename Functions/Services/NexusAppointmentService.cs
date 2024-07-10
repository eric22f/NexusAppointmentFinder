using Microsoft.Azure.ServiceBus;
using NexusAzureFunctions.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using NexusAzureFunctions.Models;

namespace NexusAzureFunctions.Services;

// This is the Primary class used to process appointments from the Nexus Appointments API
// and submit open appointments to the service bus
public class NexusAppointmentService
{
    private readonly IConfiguration _configuration;
    private readonly string _nexusAppointmentsApiUrl;
    private const int LocationId = 5020;
    private const int TotalDays = 7;
    private readonly DateTime _fromDate = DateTime.Today.AddDays(1);
    private readonly DateTime _toDate = DateTime.Today.AddDays(TotalDays + 1);
    private readonly ILogger<NexusAppointmentService> _logger;
    private readonly string _traceId;
    private readonly AppointmentCacheFactory _appointmentCacheFactory;

    public bool IsProcessAppointmentsSuccess { get; private set; }

    public NexusAppointmentService(
        ILogger<NexusAppointmentService> logger,
        IConfiguration configuration,
        Tracer tracer,
        AppointmentCacheFactory appointmentCacheFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _traceId = tracer.Id ?? throw new ArgumentNullException(nameof(tracer));
        _appointmentCacheFactory = appointmentCacheFactory ?? throw new ArgumentNullException(nameof(appointmentCacheFactory));

        _nexusAppointmentsApiUrl = _configuration["NexusApi:BaseUrl"] + _configuration["NexusApi:QueryParams"]
            ?? "https://ttp.cbp.dhs.gov/schedulerapi/locations/[LOCATION_ID]/slots?startTimestamp=[START_DATE]&endTimestamp=[END_DATE]";
        _logger.LogInformation("[{_traceId}] NexusAppointmentService initialized");
        _logger.LogInformation($"[{_traceId}] NexusAppointmentsApiUrl: {_nexusAppointmentsApiUrl}");
    }

    // This method fetches appointment data from the Nexus Appointments API
    // converts the data to a list of Appointment objects 
    // checks for available openings
    // filter down to which ones are new and have not been cached
    // submits the new open appointments to a service bus
    // cache the new appointments to prevent duplicate submissions
    // Returns all available appointments and sets IsProcessAppointmentsSuccess to true if successful
    public async Task<List<Appointment>> ProcessAppointments()
    {
        IsProcessAppointmentsSuccess = false;
        _logger.LogInformation($"[{_traceId}] ProcessAppointments started - Location ID: {LocationId} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()}");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var stopwatchTotal = new Stopwatch();
        stopwatchTotal.Start();
        List<Appointment> appointments = [];
        List<Appointment> openAppointments = [];
        try
        {
            // Fetch appointment data from API
            HttpClient httpClient = new();
            string uri = GetNexusAppointmentsApiUrl();
            var appointmentData = await httpClient.GetStringAsync(uri);
            stopwatch.Stop();
            _logger.LogTrace($"[{_traceId}] Fetching appointment data took: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();

            // Convert JSON data to list of Appointments
            var appointmentConverter = new AppointmentConverter();
            appointments = appointmentConverter.ConvertFromJson(appointmentData, LocationId);
            stopwatch.Stop();
            _logger.LogTrace($"[{_traceId}] Converting JSON data took: {stopwatch.ElapsedMilliseconds} ms to process {appointments.Count} appointments");
            stopwatch.Restart();

            // Filter appointments with openings
            openAppointments = appointments.Where(a => a.Openings > 0).ToList();
            stopwatch.Stop();
            _logger.LogTrace($"[{_traceId}] Filtering appointments took: {stopwatch.ElapsedMilliseconds} ms to locate {openAppointments.Count} available appointments");
            stopwatch.Restart();

            // Check if there are any open appointments
            if (openAppointments.Count == 0)
            {
                _logger.LogInformation($"[{_traceId}] No available appointments found.");
                IsProcessAppointmentsSuccess = true;
                return openAppointments;
            }

            // Check which appointments are new and have not been processed before            
            var appointmentsCache = _appointmentCacheFactory.CreateCacheClient();
            if (appointmentsCache != null)
            {
                openAppointments = openAppointments.Where(a => appointmentsCache.IsAppointmentNew(a)).ToList();
                stopwatch.Stop();
                _logger.LogTrace($"[{_traceId}] Checking cache for new appointments took: {stopwatch.ElapsedMilliseconds} ms");
                stopwatch.Restart();
            }
            else
            {
                _logger.LogWarning($"[{_traceId}] Cache client is not available. Not checking appointments if appointments are new.");
            }

            // Check if we should send open appointments to the service bus to trigger notifications
            if (openAppointments.Count > 0)
            {
                if (_configuration["ServiceBus:Enabled"] == "true")
                {
                    // Send open appointments to Service Bus
                    var serviceBus = ServiceBusCreator.CreateServiceBusClient(_configuration);
                    var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(openAppointments)));
                    await serviceBus.SendAsync(message);
                    stopwatch.Stop();
                    _logger.LogTrace($"[{_traceId}] Sending the new appointments to Service Bus took: {stopwatch.ElapsedMilliseconds} ms");
                }
                else
                {
                    _logger.LogInformation($"[{_traceId}] ServiceBus is disabled. Not sending new available appointments.");
                }
            }
            else
            {
                _logger.LogInformation($"[{_traceId}] No new appointments found.");
            }

            // See if we should cache the open appointments
            if (appointmentsCache != null)
            {
                // Cache the open appointments
                stopwatch.Stop();
                appointmentsCache.CacheAppointments(LocationId, _fromDate, _toDate, openAppointments);
            }

            IsProcessAppointmentsSuccess = true;
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            _logger.LogError($"[{_traceId}] An error occurred while processing appointments: {ex.Message}");
        }
        finally
        {
            stopwatchTotal.Stop();
            _logger.LogInformation($"[{_traceId}] ProcessAppointments took: {stopwatchTotal.ElapsedMilliseconds} ms to process {appointments.Count} appointments and found {openAppointments.Count} available appointments for location ID: {LocationId} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()}");
        }
        return openAppointments;
    }

    // Get the URL for the Nexus Appointments API based on Location and start and end dates
    private string GetNexusAppointmentsApiUrl()
    {
        return _nexusAppointmentsApiUrl.Replace("[LOCATION_ID]", LocationId.ToString())
            .Replace("[START_DATE]", _fromDate.ToString("yyyy-MM-ddT00:00:00"))
            .Replace("[END_DATE]", _toDate.ToString("yyyy-MM-ddT00:00:00"));
    }
}