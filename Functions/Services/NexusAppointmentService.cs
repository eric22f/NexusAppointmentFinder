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
    private readonly int _locationId;
    private readonly int _totalDays;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
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
        _traceId = tracer?.Id ?? throw new ArgumentNullException(nameof(tracer));
        _appointmentCacheFactory = appointmentCacheFactory ?? throw new ArgumentNullException(nameof(appointmentCacheFactory));

        _locationId = _configuration.GetValue<int>("NexusApi:LocationId");
        if (_locationId < 1)
        {
            throw new ArgumentException("NexusAPI Location ID is required to process appointments.");
        }
        _totalDays = _configuration.GetValue<int>("NexusApi:TotalDays");
        if (_totalDays < 1)
        {
            _totalDays = 7;
        }
        _fromDate = DateTime.Today.AddDays(1);
        _toDate = DateTime.Today.AddDays(_totalDays + 1);
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
        _logger.LogInformation($"[{_traceId}] ProcessAppointments started - Location ID: {_locationId} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()}");
        var stopwatch = Stopwatch.StartNew();
        var stopwatchTotal = Stopwatch.StartNew();
        List<Appointment> appointments = [];
        List<Appointment> openAppointments = [];
        try
        {
            // Fetch appointment data from API
            string appointmentData = await PullAppointmentDataFromNexusApi(stopwatch);

            // Convert JSON data to list of Appointments
            appointments = ConvertNexusApiResultsToAppointmentsList(stopwatch, appointmentData);

            // Filter appointments with openings
            openAppointments = FilterToAvailableAppointments(stopwatch, appointments);

            // Check if there are any open appointments
            if (openAppointments.Count == 0)
            {
                _logger.LogInformation($"[{_traceId}] No available appointments found.");
                IsProcessAppointmentsSuccess = true;
                return openAppointments;
            }

            // Check which appointments are new and have not been processed before            
            AppointmentCacheBase? appointmentsCache = CheckCacheForNewAppointments(stopwatch, ref openAppointments);

            // Check if we should send open appointments to the service bus to trigger notifications
            if (openAppointments.Count > 0)
            {
                await SubmitNewAppointmentsForNotifications(stopwatch, openAppointments);
            }
            else
            {
                _logger.LogInformation($"[{_traceId}] No new appointments found.");
            }

            // Cache the new open appointments
            CacheNewAppointments(stopwatch, openAppointments, appointmentsCache);

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
            _logger.LogInformation($"[{_traceId}] ProcessAppointments took: {stopwatchTotal.ElapsedMilliseconds} ms to process {appointments.Count} appointments and found {openAppointments.Count} available appointments for location ID: {_locationId} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()}");
        }
        return openAppointments;
    }

#region Private Methods
    // Fetch appointment data from Nexus Appointments API
    private async Task<string> PullAppointmentDataFromNexusApi(Stopwatch stopwatch)
    {
        HttpClient httpClient = new();
        string uri = GetNexusAppointmentsApiUrl();
        var appointmentData = await httpClient.GetStringAsync(uri);
        stopwatch.Stop();
        _logger.LogTrace($"[{_traceId}] Fetching appointment data took: {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Restart();
        return appointmentData;
    }

    // Get the URL for the Nexus Appointments API based on Location and start and end dates
    private string GetNexusAppointmentsApiUrl()
    {
        return _nexusAppointmentsApiUrl.Replace("[LOCATION_ID]", _locationId.ToString())
            .Replace("[START_DATE]", _fromDate.ToString("yyyy-MM-ddT00:00:00"))
            .Replace("[END_DATE]", _toDate.ToString("yyyy-MM-ddT00:00:00"));
    }


    // Convert JSON data to list of Appointments
    private List<Appointment> ConvertNexusApiResultsToAppointmentsList(Stopwatch stopwatch, string appointmentData)
    {
        List<Appointment> appointments;
        var appointmentConverter = new AppointmentConverter();
        appointments = appointmentConverter.ConvertFromJson(appointmentData, _locationId);
        stopwatch.Stop();
        _logger.LogTrace($"[{_traceId}] Converting JSON data took: {stopwatch.ElapsedMilliseconds} ms to process {appointments.Count} appointments");
        stopwatch.Restart();
        return appointments;
    }

    // Filter appointments with openings
    private List<Appointment> FilterToAvailableAppointments(Stopwatch stopwatch, List<Appointment> appointments)
    {
        List<Appointment> openAppointments = appointments.Where(a => a.Openings > 0).ToList();
        stopwatch.Stop();
        _logger.LogTrace($"[{_traceId}] Filtering appointments took: {stopwatch.ElapsedMilliseconds} ms to locate {openAppointments.Count} available appointments");
        stopwatch.Restart();
        return openAppointments;
    }

    // Check which appointments are new and have not been processed before
    // Returns the cache client if available
    private AppointmentCacheBase? CheckCacheForNewAppointments(Stopwatch stopwatch, ref List<Appointment> openAppointments)
    {
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

        return appointmentsCache;
    }

    // Send open appointments to Service Bus
    private async Task SubmitNewAppointmentsForNotifications(Stopwatch stopwatch, List<Appointment> openAppointments)
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

    // Cache the new open appointments if the cache client is available
    private void CacheNewAppointments(Stopwatch stopwatch, List<Appointment> openAppointments, AppointmentCacheBase? appointmentsCache)
    {
        if (appointmentsCache != null)
        {
            // Cache the open appointments
            stopwatch.Stop();
            appointmentsCache.CacheAppointments(_locationId, _fromDate, _toDate, openAppointments);
        }
    }
#endregion
}