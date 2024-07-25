
using System.Text;

namespace NexusAzureFunctions.Helpers;

// Genereates a unique Id for logging
public class Tracer
{
    private readonly string _traceId;

    public Tracer()
    {
        _traceId = InsertDashes(Guid.NewGuid().ToString("N")[..9], 5);
    }

    // Unique Id for logging
    public string Id { get { return _traceId; } }

    // Function to insert dashes after every n characters in a string
    private static string InsertDashes(string input, int n)
    {
        StringBuilder sb = new ();
        for (int i = 0; i < input.Length; i++)
        {
            sb.Append(input[i]);
            if ((i + 1) % n == 0 && i != input.Length - 1)
            {
                sb.Append('-');
            }
        }
        return sb.ToString();
    }
}