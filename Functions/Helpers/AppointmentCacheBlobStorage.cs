using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.Json;
using NexusAzureFunctions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Configuration;
using Azure.Identity;

namespace NexusAzureFunctions.Helpers
{
    public class AppointmentCacheBlobStorage : AppointmentCacheBase
    {
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ILogger<AppointmentCacheBlobStorage> _logger;
        private readonly Tracer _tracer;

        public AppointmentCacheBlobStorage(IConfiguration configuration, ILogger<AppointmentCacheBlobStorage> logger,
            Tracer tracer)
        {
            var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            string accountName = config["BlobStorage:StorageAccountName"] + "";
            BlobServiceClient blobServiceClient;
            if (string.IsNullOrEmpty(accountName))
            {
                string blobConnectionsString = config["AzureWebJobsStorage"] ??
                    config["Values:AzureWebJobsStorage"] ?? 
                    throw new ConfigurationErrorsException("Configuration setting 'AzureWebJobsStorage' not found.");
                blobServiceClient = new BlobServiceClient(blobConnectionsString);
            }
            else
            {
                blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new DefaultAzureCredential());
            }
            string blobContainerName = config["BlobStorage:ContainerName"] ?? 
                throw new ConfigurationErrorsException("Configuration setting 'BlobStorage:ContainerName' not found.");
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        // Add the appointment to the Blob Storage
        public override void CacheAppointments(int locationId, DateTime startDate, DateTime endDate, List<Appointment> appointments)
        {
            string blobName = $"Appointments_LocationId_{locationId}.json";
            try
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);

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
                blobClient.DeleteIfExists();
                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                {
                    blobClient.Upload(stream, overwrite: true);
                }

                _logger.LogInformation($"[_tracer.Id] {cachedAppointments.Count} Appointments cached to blob: {blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[_tracer.Id] Error caching appointments to Blob Storage '{blobName}': {ex.Message}");
                throw;
            }
        }

        // Get the appointments from the Blob Storage by location and date range
        protected override List<Appointment> GetCachedAppointments(int locationId, DateTime startDate, DateTime endDate)
        {
            List<Appointment> appointments = GetCachedAppointments(locationId);
            return appointments.Where(a => a.Date >= startDate && a.Date.Date <= endDate).ToList();
        }

        // Clear the appointment cache for a location
        public override void ClearCache(int locationId)
        {
            string blobName = $"Appointments_LocationId_{locationId}.json";
            try
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
                blobClient.DeleteIfExists();
                _logger.LogInformation($"[_tracer.Id] Appointment cache cleared for blob: {blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[_tracer.Id] Error clearing appointment cache to Blob Storage '{blobName}': {ex.Message}");
                throw;
            }            
        }

        #region Private Methods
        // Get the all open appointments from the Blob Storage by location
        private List<Appointment> GetCachedAppointments(int locationId)
        {
            string blobName = $"Appointments_LocationId_{locationId}.json";
            try
            {
                BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);

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
                _logger.LogError($"[_tracer.Id] Error retrieving cached appointments from Blob Storage '{blobName}': {ex.Message}");
                throw;
            }
        }
    }
#endregion
}
