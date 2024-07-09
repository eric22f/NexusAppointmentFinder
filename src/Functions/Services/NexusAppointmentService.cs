using Microsoft.Azure.ServiceBus;
using Functions.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using Functions.Models;

namespace Functions.Services
{
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
        private readonly ILogger _logger;
        private readonly string _traceId;

        public NexusAppointmentService(Tracer tracer)
        {
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            _logger = new LoggerFactory().CreateLogger<NexusAppointmentService>();
        
            _traceId = tracer?.Id ?? throw new ArgumentNullException(nameof(tracer));
        
            _nexusAppointmentsApiUrl = _configuration["NexusAppointmentsApiUrl"] 
                ?? "https://ttp.cbp.dhs.gov/schedulerapi/locations/[LOCATION_ID]/slots?startTimestamp=[START_DATE]&endTimestamp=[END_DATE]";
            _logger.LogInformation("[{_traceId}]NexusAppointmentService initialized");
            _logger.LogInformation("[{_traceId}]NexusAppointmentsApiUrl: {_nexusAppointmentsApiUrl}", _nexusAppointmentsApiUrl);
        }

        // This method fetches appointment data from the Nexus Appointments API
        // converts to a list of Appointment objects 
        // checks for new openings
        // then submits the new open appointments to the service bus
        // Returns the number of appointments processed or -1 if an error occurred
        public async Task<int> ProcessAppointments()
        {
            _logger.LogTrace($"[{_traceId}]ProcessAppointments started - Location ID: {LocationId} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var stopwatchTotal = new Stopwatch();
            stopwatchTotal.Start();
            var appointments = new List<Appointment>();
            try
            {
                // Fetch appointment data from API
                HttpClient httpClient = new();
                string uri = GetNexusAppointmentsApiUrl();
                var appointmentData = await httpClient.GetStringAsync(uri);
                stopwatch.Stop();
                _logger.LogTrace($"[{_traceId}]Fetching appointment data took: {stopwatch.ElapsedMilliseconds} ms");
                stopwatch.Restart();

                // Convert JSON data to list of Appointments
                var appointmentConverter = new AppointmentConverter();
                appointments = appointmentConverter.ConvertFromJson(appointmentData, LocationId);
                stopwatch.Stop();
                _logger.LogTrace($"[{_traceId}]Converting JSON data took: {stopwatch.ElapsedMilliseconds} ms to process {appointments.Count} appointments");
                stopwatch.Restart();

                // Filter appointments with openings
                var openAppointments = appointments.Where(a => a.Openings > 0).ToList();
                stopwatch.Stop();
                _logger.LogTrace($"[{_traceId}]Filtering appointments took: {stopwatch.ElapsedMilliseconds} ms to locate {openAppointments.Count} available appointments");
                stopwatch.Restart();

                // Check which appointments are new and have not been processed before
                var appointmentsCache = AppointmentCacheFactory.CreateCacheClient(_configuration);
                openAppointments = openAppointments.Where(a => appointmentsCache.IsAppointmentNew(a)).ToList();
                stopwatch.Stop();
                _logger.LogTrace($"[{_traceId}]Checking cache for new appointments took: {stopwatch.ElapsedMilliseconds} ms");
                stopwatch.Restart();

                // Send open appointments to Service Bus
                var serviceBus = ServiceBusCreator.CreateServiceBusClient(_configuration);
                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(openAppointments)));
                await serviceBus.SendAsync(message);
                stopwatch.Stop();
                _logger.LogTrace($"[{_traceId}]Sending the new appointments to Service Bus took: {stopwatch.ElapsedMilliseconds} ms");

                // Cache the open appointments
                stopwatch.Stop();
                appointmentsCache.CacheAppointments(LocationId, _fromDate, _toDate, openAppointments);

                return openAppointments.Count;
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                _logger.LogError($"[{_traceId}]An error occurred while processing appointments: {ex.Message}");
                return -1;
            }
            finally
            {
                stopwatchTotal.Stop();
                _logger.LogInformation($"[{_traceId}]ProcessAppointments took: {stopwatchTotal.ElapsedMilliseconds} ms to process {appointments.Count} appointments and found {openAppointments.Count} available appointments for location ID: {LocationId} from {_fromDate.ToShortDateString()} to {_toDate.ToShortDateString()}");
            }
        }

        // Get the URL for the Nexus Appointments API based on Location and start and end dates
        private string GetNexusAppointmentsApiUrl()
        {
            return _nexusAppointmentsApiUrl.Replace("[LOCATION_ID]", LocationId.ToString())
                .Replace("[START_DATE]", _fromDate.ToString("yyyy-MM-ddT00:00:00"))
                .Replace("[END_DATE]", _toDate.ToString("yyyy-MM-ddT00:00:00"));
        }
    }
}