
namespace NexusAzureFunctionsTests.Models;

// This class is used as the json model from the Nexus API
// Example: https://ttp.cbp.dhs.gov/schedulerapi/locations/5020/slots?startTimestamp=2024-07-23T00:00&endTimestamp=2024-07-24T00:00
public class HttpAppointment
{
    public int active { get; set; }
    public int total { get; set; }
    public int pending { get; set; }
    public int conflicts { get; set; }
    public int duration { get; set; }
    private string _timestamp = "";
    public string timestamp 
    {
        get { return _timestamp; }
        set 
        { 
            _timestamp = value;
            if (DateTime.TryParse(_timestamp, out DateTime result))
            {
                _timestamp = result.ToString("yyyy-MM-ddTHH:mm");
            }
        }
    }
    public bool remote { get; set; }
}
