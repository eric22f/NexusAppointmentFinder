using System.Diagnostics;

namespace NexusAzureFunctions.Models
{
    // Nexus Appointment result
    [DebuggerDisplay("Appointment: {ToString()}")]
    public class Appointment
    {
        public int LocationId { get; set; }
        public DateTime Date { get; set; }
        public int Openings { get; set; }
        public int TotalSlots { get; set; }
        public int Pending { get; set; }
        public int Conflicts { get; set; }
        public int Duration { get; set; }

        public string AppointmentKey => $"{LocationId}-{Date.ToString("yyyy-MM-ddTHH:mm")}";
        public static string GetAppointmentDetails(Appointment appointment)
        {
            return $"Location: {appointment.LocationId}, Date: {appointment.Date.ToString("yyyy-MM-ddTHH:mm")}, Openings: {appointment.Openings}, TotalSlots: {appointment.TotalSlots}, Pending: {appointment.Pending}, Conflicts: {appointment.Conflicts}, Duration: {appointment.Duration}";
        }
        // Override the Equals method
        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            Appointment appointment = (Appointment)obj;
            return LocationId == appointment.LocationId && Date == appointment.Date;
        }
        // Override the GetHashCode method
        public override int GetHashCode()
        {
            return HashCode.Combine(LocationId, Date);
        }
        public override string ToString()
        {
            return GetAppointmentDetails(this);
        }
    }
}