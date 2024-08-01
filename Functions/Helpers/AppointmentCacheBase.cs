using NexusAzureFunctions.Models;
using Microsoft.Extensions.Logging;

namespace NexusAzureFunctions.Helpers;

// This class is used to cache appointments in order to prevent duplicate processing
public abstract class AppointmentCacheBase
{
    private readonly object _lock = new();
    protected Dictionary<string, Appointment>? _appointmentsCache;
    protected abstract List<Appointment> GetCachedAppointments(int locationId, DateTime startDate, DateTime endDate);
    public abstract void CacheAppointments(int locationId, DateTime startDate, DateTime endDate, List<Appointment> appointments);
    public abstract void ClearCache(int locationId);

    // Load appointments from cache
    public void LoadCachedAppointments(int locationId, DateTime startDate, DateTime endDate)
    {
        lock (_lock)
        {
            _appointmentsCache = [];
            List<Appointment> appointments = GetCachedAppointments(locationId, startDate, endDate);
            foreach (Appointment appointment in appointments)
            {
                _appointmentsCache[appointment.AppointmentKey] = appointment;
            }
        }
    }
 
    // Check if this appointment is not in the cache
    // LoadCachedAppointments must be called before this method
    public virtual bool IsAppointmentNew(Appointment appointment) 
    { 
        if (_appointmentsCache == null)
        {
            throw new InvalidOperationException("Appointments cache is not initialized.");
        }
        return !_appointmentsCache.ContainsKey(appointment.AppointmentKey);
    }

    // Get the appointments from local cache
    public List<Appointment> GetCachedAppointments()
    {
        if (_appointmentsCache == null)
        {
            throw new InvalidOperationException("Appointments cache is not initialized.");
        }
        return [.. _appointmentsCache.Values];
    }
}