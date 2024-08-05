using Microsoft.Azure.ServiceBus;
using NexusAzureFunctions.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using NexusAzureFunctions.Models;
using Azure.Messaging.ServiceBus;

namespace NexusAzureFunctions.Services;

// This is the Primary class used to process appointments from the Nexus Appointments API
// and submit open appointments to the service bus
public class NexusAppointmentService
{
    private readonly IConfiguration _configuration;
    private readonly string _nexusAppointmentsApiUrl;
    private readonly int _locationId;
    private int _totalDays;
    private readonly DateTime _fromDate;
    private readonly DateTime _toDate;
    private readonly ILogger<NexusAppointmentService> _logger;
    private readonly string _traceId;
    private readonly AppointmentCacheFactory _appointmentCacheFactory;
    private readonly HttpClient _httpClient;
    private AppointmentCacheBase? _appointmentsCache;

    public bool IsProcessAppointmentsSuccess { get; private set; }
    public string FriendlyErrorMessage { get; private set; } = "";
    public int TotalDays => _totalDays;

    public NexusAppointmentService(
        ILogger<NexusAppointmentService> logger,
        IConfiguration configuration,
        Tracer tracer,
        AppointmentCacheFactory appointmentCacheFactory,
        HttpClient? httpClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _traceId = tracer?.Id ?? throw new ArgumentNullException(nameof(tracer));
        _appointmentCacheFactory = appointmentCacheFactory ?? throw new ArgumentNullException(nameof(appointmentCacheFactory));
        _httpClient = httpClient ?? new HttpClient();

        _locationId = _configuration.GetValue<int>("NexusApi:LocationId");
        _totalDays = _configuration.GetValue<int>("NexusApi:TotalDays");
        _fromDate = DateTime.Today.AddDays(1);
        _toDate = _fromDate.AddDays(_totalDays);
        _nexusAppointmentsApiUrl = _configuration["NexusApi:BaseUrl"] + _configuration["NexusApi:QueryParams"]
            ?? "https://ttp.cbp.dhs.gov/schedulerapi/locations/[LOCATION_ID]/slots?startTimestamp=[START_DATE]&endTimestamp=[END_DATE]";
        _logger.LogInformation($"[{_traceId}] NexusAppointmentService initialized: {_nexusAppointmentsApiUrl}");
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
        FriendlyErrorMessage = "";
        if (_locationId == 0)
        {
            throw new InvalidOperationException($"[{_traceId}] Location ID is not set. Cannot process appointments.");
        }
        if (_totalDays < 1)
        {
            _totalDays = 7;
            _logger.LogWarning($"[{_traceId}] TotalDays is not set or invalid. Defaulting to {_totalDays} days.");
        }
        IsProcessAppointmentsSuccess = false;
        _logger.LogInformation($"[{_traceId}] ProcessAppointments started - Location ID: {_locationId} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()}");
        var stopwatchTotal = Stopwatch.StartNew();
        List<Appointment> appointments = [];
        List<Appointment> openAppointments = [];
        try
        {
            // Fetch appointment data from API
            string appointmentData = await PullAppointmentDataFromNexusApi();

            // Convert JSON data to list of Appointments
            appointments = ConvertNexusApiResultsToAppointmentsList(appointmentData);

            // Filter appointments with openings
            openAppointments = FilterToAvailableAppointments(appointments);

            // Check if there are any open appointments
            if (openAppointments.Count == 0)
            {
                IsProcessAppointmentsSuccess = true;
                return openAppointments;
            }

            // Get the cache client
            _appointmentsCache = _appointmentCacheFactory.CreateCacheClient();

            // Check which appointments are new and have not been processed before            
            var newAppointments = CheckCacheForNewAppointments(openAppointments);

            // Check if we should send open appointments to the service bus to trigger notifications
            if (newAppointments.Count > 0)
            {
                await SubmitNewAppointmentsForNotifications(newAppointments);
            }

            // Cache open appointments
            CacheOpenAppointments(openAppointments);

            IsProcessAppointmentsSuccess = true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"[{_traceId}] Error fetching appointment data from Nexus API.");
            FriendlyErrorMessage = "A problem occured pulling appointmentd data from the Nexus Website.  Please try again later.";
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            _logger.LogError(ex, $"[{_traceId}] An error occurred while processing appointments.");
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
    private async Task<string> PullAppointmentDataFromNexusApi()
    {
        var stopwatch = Stopwatch.StartNew();
        string uri = GetNexusAppointmentsApiUrl();
        var appointmentData = await _httpClient.GetStringAsync(uri);
        stopwatch.Stop();
        _logger.LogTrace($"[{_traceId}] Fetching appointment data took: {stopwatch.ElapsedMilliseconds} ms\nFrom: {uri}");
        return appointmentData;
    }

    // Get the URL for the Nexus Appointments API based on Location and start and end dates
    private string GetNexusAppointmentsApiUrl()
    {
        return _nexusAppointmentsApiUrl.Replace("[LOCATION_ID]", _locationId.ToString())
            .Replace("[START_DATE]", _fromDate.ToString("yyyy-MM-ddT00:00"))
            .Replace("[END_DATE]", _toDate.ToString("yyyy-MM-ddT00:00"));
    }


    // Convert JSON data to list of Appointments
    private List<Appointment> ConvertNexusApiResultsToAppointmentsList(string appointmentData)
    {
        var stopwatch = Stopwatch.StartNew();
        List<Appointment> appointments;
        var appointmentConverter = new AppointmentConverter();
        appointments = appointmentConverter.ConvertFromJson(appointmentData, _locationId);
        stopwatch.Stop();
        _logger.LogTrace($"[{_traceId}] Converting JSON data took: {stopwatch.ElapsedMilliseconds} ms to process {appointments.Count} appointments");
        return appointments;
    }

    // Filter appointments with openings
    private List<Appointment> FilterToAvailableAppointments(List<Appointment> appointments)
    {
        var stopwatch = Stopwatch.StartNew();
        List<Appointment> openAppointments = appointments.Where(a => a.Openings > 0).ToList();
        stopwatch.Stop();
        _logger.LogTrace($"[{_traceId}] Filtering appointments took: {stopwatch.ElapsedMilliseconds} ms to locate {openAppointments.Count} available appointments");
        return openAppointments;
    }

    // Check which appointments are new and have not been processed before
    // If a cache client is not available then all appointments are considered new
    private List<Appointment> CheckCacheForNewAppointments(List<Appointment> openAppointments)
    {
        List<Appointment> newAppointments = [];
        if (_appointmentsCache != null)
        {
            var stopwatch = Stopwatch.StartNew();
            _appointmentsCache.LoadCachedAppointments(_locationId, _fromDate, _toDate);
            newAppointments = openAppointments.Where(a => _appointmentsCache.IsAppointmentNew(a)).ToList();
            stopwatch.Stop();
            _logger.LogTrace($"[{_traceId}] Checking cache for {newAppointments.Count} new appointments took: {stopwatch.ElapsedMilliseconds} ms");
        }
        else
        {
            _logger.LogWarning($"[{_traceId}] Cache client is not available. Not checking if appointments are new.");
            newAppointments = openAppointments;
        }

        return newAppointments;
    }

    // Send new open appointments to Service Bus
    private async Task SubmitNewAppointmentsForNotifications(List<Appointment> newAppointments)
    {
        if (_configuration["ServiceBus:Enabled"] == "true")
        {
            var serviceBus = ServiceBusCreator.CreateServiceBusClient(_configuration);
            var stopwatch = Stopwatch.StartNew();

            var chunkedAppointments = ChunkAppointments(newAppointments, 128 * 1024); // 128 KB (max message size)

            for (int i = 0; i < chunkedAppointments.Count; i++)
            {
                var chunk = chunkedAppointments[i];
                await SendMessageAsync(serviceBus, chunk, i, chunkedAppointments.Count);
            }
 
            stopwatch.Stop();
            _logger.LogTrace($"[{_traceId}] Sending {newAppointments.Count} new appointments to Service Bus took: {stopwatch.ElapsedMilliseconds} ms");
        }
        else
        {
            _logger.LogInformation($"[{_traceId}] ServiceBus is disabled. Not sending new available appointments.");
        }
    }

    // Split appointments into chunks to send to Service Bus
    // to prevent exceeding message size limits
    private static List<List<Appointment>> ChunkAppointments(List<Appointment> appointments, int maxMessageSize)
    {
        var chunkedAppointments = new List<List<Appointment>>();
        var currentChunk = new List<Appointment>();
        var currentChunkSize = 0;

        foreach (var appointment in appointments)
        {
            var appointmentSize = Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(appointment));
            if (currentChunkSize + appointmentSize > maxMessageSize && currentChunk.Count > 0)
            {
                chunkedAppointments.Add(currentChunk);
                currentChunk = new List<Appointment>();
                currentChunkSize = 0;
            }
            currentChunk.Add(appointment);
            currentChunkSize += appointmentSize;
        }

        if (currentChunk.Count > 0)
        {
            chunkedAppointments.Add(currentChunk);
        }

        return chunkedAppointments;
    }

    // Send message to Service Bus
    private async Task SendMessageAsync(QueueClient serviceBus, List<Appointment> chunk, int index, int total)
    {
        var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(chunk));
        var plural = chunk.Count > 1 ? "s" : "";
        var indexStr = total > 1 ? $"-({index + 1}/{total})" : "";
        var message = new Message(messageBody)
        {
            MessageId = _traceId + indexStr,
            ContentType = "application/json",
            CorrelationId = _traceId,
            Label = $"{chunk.Count} new appointment{plural}"
        };

        await serviceBus.SendAsync(message);
    }


    // Cache the all open appointments if the cache client is available
    private void CacheOpenAppointments(List<Appointment> openAppointments)
    {
        if (_appointmentsCache != null)
        {
            // Cache the open appointments
            var stopwatch = Stopwatch.StartNew();
            _appointmentsCache.CacheAppointments(_locationId, _fromDate, _toDate, openAppointments);
            stopwatch.Stop();
            _logger.LogTrace($"[{_traceId}] Caching {openAppointments.Count} open appointments took: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
#endregion
}