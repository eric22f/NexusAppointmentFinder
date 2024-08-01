

using System.Configuration;
using Microsoft.Extensions.Configuration;
using NexusAzureFunctions.Models;

namespace NexusAzureFunctions.Helpers;

// This class is used to manage user confuguration settings
// Including user notification preferences and location assignments
public class NexusManager(IConfiguration config, NexusBlob? nexusBlob = null, NexusDB? nexusDB = null)
{
    private readonly bool _useBlobStorage = (config["BlobStorage:Enabled"] == "true" && config["SqlDatabase:Enabled"] != "true");
    private readonly NexusDB? _nexusDB = nexusDB;
    private readonly NexusBlob? _nexusBlob = nexusBlob;

    // Get all users that have registered for this location
    public List<UserNotifications> GetUsersAssignedToLocation(int locationId)
    {
        if (_useBlobStorage && _nexusBlob != null)
        {
            return _nexusBlob.GetUsersAssignedToLocation(locationId);
        }
        if (_nexusDB != null)
        {
            return _nexusDB.GetUsersAssignedToLocation(locationId);
        }
        throw new ConfigurationErrorsException("Configuration error: either 'SqlDatabase:Enabled' or 'BlobStorage:Enabled' must be true.");
    }
}