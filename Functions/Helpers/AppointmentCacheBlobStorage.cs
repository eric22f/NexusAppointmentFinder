using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;
using NexusAzureFunctions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Configuration;

namespace NexusAzureFunctions.Helpers
{
    public class AppointmentCacheBlobStorage : AppointmentCacheBase
    {
        private readonly string _blobContainerName;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<AppointmentCacheBlobStorage> _logger;
        private readonly Tracer _tracer;

        public AppointmentCacheBlobStorage(IConfiguration configuration, ILogger<AppointmentCacheBlobStorage> logger,
            Tracer tracer)
        {
            var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _blobContainerName = config["BlobStorage:ContainerName"] ?? 
                throw new ConfigurationErrorsException("Configuration setting 'BlobStorage:ContainerName' not found.");
            var blobStorageConnectionString = config["BlobStorage:BlobStorageConnectionString"] ?? 
                throw new ConfigurationErrorsException("Configuration setting 'BlobStorage:BlobStorageConnectionString' not found.");
            _blobServiceClient = new BlobServiceClient(blobStorageConnectionString);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        // Add the appointment to the Blob Storage
        public override void CacheAppointments(int locationId, DateTime startDate, DateTime endDate, List<Appointment> appointments)
        {
            try
            {
                string blobName = $"Appointments_LocationId_{locationId}.json";
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Get current appointments that have been cached
                // Remove older appointments that have already passed
                // Remove appointments that are within the date range
                // Add the new appointments
                var cachedAppointments = GetCachedAppointments(locationId);
                cachedAppointments.RemoveAll(a => a.Date <= DateTime.Now);
                cachedAppointments.RemoveAll(a => a.Date >= startDate && a.Date.Date <= endDate);
                cachedAppointments.AddRange(appointments);
                cachedAppointments.Sort((a, b) => a.Date.CompareTo(b.Date));

                var json = JsonSerializer.Serialize(cachedAppointments);
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                {
                    blobClient.Upload(stream, overwrite: true);
                }

                _logger.LogInformation($"[_tracer.Id] {cachedAppointments.Count} Appointments cached to blob: {blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[_tracer.Id] Error caching appointments to Blob Storage: {ex.Message}");
                throw;
            }
        }

        // Get the appointments from the Blob Storage by location and date range
        protected override List<Appointment> GetCachedAppointments(int locationId, DateTime startDate, DateTime endDate)
        {
            try
            {
                List<Appointment> appointments = GetCachedAppointments(locationId);
                return appointments.Where(a => a.Date >= startDate && a.Date.Date <= endDate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[_tracer.Id] Error retrieving cached appointments from Blob Storage: {ex.Message}");
                throw;
            }
        }

        // Get the all open appointments from the Blob Storage by location
        private List<Appointment> GetCachedAppointments(int locationId)
        {
            try
            {
                string blobName = $"Appointments_LocationId_{locationId}.json";
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                if (blobClient.Exists())
                {
                    var downloadInfo = blobClient.DownloadContent();
                    var json = downloadInfo.Value.Content.ToString();
                    var deserializedAppointments = JsonSerializer.Deserialize<List<Appointment>>(json);
                    return deserializedAppointments ?? [];
                }
                else
                {
                    _logger.LogWarning($"[_tracer.Id] No cached appointments found in blob: {blobName}");
                    return [];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[_tracer.Id] Error retrieving cached appointments from Blob Storage: {ex.Message}");
                throw;
            }
        }
    }
}
