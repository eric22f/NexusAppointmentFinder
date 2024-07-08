namespace Functions.Models
{
    // Nexus Appointment result
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
        public string AppointmentDetails => $"Location: {LocationId}, Date: {Date.ToString("yyyy-MM-ddTHH:mm")}, Openings: {Openings}, TotalSlots: {TotalSlots}, Pending: {Pending}, Conflicts: {Conflicts}, Duration: {Duration}";
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
    }
}