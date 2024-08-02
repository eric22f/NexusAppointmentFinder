using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using System.Configuration;
using NexusAzureFunctions.Models;
using System.Text.Json;

namespace NexusAzureFunctions.Helpers;

public class NexusBlob
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly BlobContainerClient? _blobContainerClient;
    private readonly string _blobStorageName;

    public NexusBlob(IConfiguration config)
    {
        if (config["BlobStorage:Enabled"] == "true")
        {
            string blobConnectionsString = config["BlobStorage:BlobStorageConnectionString"] ?? 
                throw new ConfigurationErrorsException("Configuration setting 'BlobStorage:BlobStorageConnectionString' not found.");
            _blobServiceClient = new BlobServiceClient(blobConnectionsString);
            string blobContainerName = config["BlobStorage:ContainerName"] ?? 
                throw new ConfigurationErrorsException("Configuration setting 'BlobStorage:ContainerName' not found.");
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainerName);
        }
        else
        {
            _blobServiceClient = null;
            _blobContainerClient = null;
        }
        _blobStorageName = "UserNotifications.json";
    }

    // Get the users assigned to a location
    public List<UserNotifications> GetUsersAssignedToLocation(int locationId) 
    {
        List<UserNotifications> userNotifications = [];
        BlobClient? blobClient = _blobContainerClient?.GetBlobClient(_blobStorageName);

        if (blobClient != null && blobClient.Exists())
        {
            using var memoryStream = new MemoryStream();
            blobClient.DownloadTo(memoryStream);
            memoryStream.Position = 0;
            using var streamReader = new StreamReader(memoryStream);
            string json = streamReader.ReadToEnd();
            userNotifications = JsonSerializer.Deserialize<List<UserNotifications>>(json) ?? [];
        }

        // Filter to the location id
        userNotifications = userNotifications.Where(u => u.LocationId == locationId).ToList();
        return userNotifications;
    }
}