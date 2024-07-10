using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Functions.Models;

namespace Functions.Helpers
{
    // Convert Nexus appointments Json results to Appointment model
    public class AppointmentConverter : JsonConverter<Appointment>
    {
        public int LocationId { get; set; }
        public override bool CanWrite => false;
        public List<Appointment> ConvertFromJson(string json, int locationId)
        {
            LocationId = locationId;
            return JsonConvert.DeserializeObject<List<Appointment>>(json, this) ?? [];
        }

        public override Appointment ReadJson(JsonReader reader, Type objectType, Appointment? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader) ?? throw new JsonSerializationException("Error deserializing Appointment: JObject is null.");
            var appointment = new Appointment
            {
                Date = jObject["timestamp"]?.ToObject<DateTime>() ?? throw new JsonSerializationException("Error deserializing Appointment: Missing or invalid 'timestamp' value."),
                Openings = jObject["active"]?.ToObject<int>() ?? throw new JsonSerializationException("Error deserializing Appointment: Missing or invalid 'active' value."),
                TotalSlots = jObject["total"]?.ToObject<int>() ?? throw new JsonSerializationException("Error deserializing Appointment: Missing or invalid 'total' value."),
                Pending = jObject["pending"]?.ToObject<int>() ?? throw new JsonSerializationException("Error deserializing Appointment: Missing or invalid 'pending' value."),
                Conflicts = jObject["conflicts"]?.ToObject<int>() ?? throw new JsonSerializationException("Error deserializing Appointment: Missing or invalid 'conflicts' value."),
                Duration = jObject["duration"]?.ToObject<int>() ?? throw new JsonSerializationException("Error deserializing Appointment: Missing or invalid 'duration' value."),
                LocationId = this.LocationId,
            };

            return appointment;
        }

        public override void WriteJson(JsonWriter writer, Appointment? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}