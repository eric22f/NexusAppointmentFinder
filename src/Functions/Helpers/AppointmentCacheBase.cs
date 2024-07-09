using Functions.Models;
using Microsoft.Extensions.Logging;

namespace Functions.Helpers
{
    // This class is used to cache appointments in order to prevent duplicate processing
    public abstract class AppointmentCacheBase
    {
        public abstract List<Appointment> GetCachedAppointments(int locationId, DateTime startDate, DateTime endDate);
        public abstract bool IsAppointmentNew(Appointment appointment);
        public abstract void CacheAppointments(int locationId, DateTime startDate, DateTime endDate, List<Appointment> appointments);

        // Group appointments by date
        protected static Dictionary<DateTime, List<Appointment>> GetAppointmentsByDate(List<Appointment> appointments)
        {
            Dictionary<DateTime, List<Appointment>> appointmentsByDate = new();
            foreach (Appointment appointment in appointments)
            {
                if (!appointmentsByDate.ContainsKey(appointment.Date))
                {
                    appointmentsByDate[appointment.Date] = new List<Appointment>();
                }
                appointmentsByDate[appointment.Date].Add(appointment);
            }
            return appointmentsByDate;
        }
    }
}