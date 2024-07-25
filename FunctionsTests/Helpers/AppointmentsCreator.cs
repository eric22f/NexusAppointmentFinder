
using NexusAzureFunctions.Models;

namespace NexusAzureFunctionsTests.Helpers;

public static class AppointmentsCreator
{
    public static List<Appointment> CreateAppointmentsList(int scenerioId, DateTime startDate, DateTime endDate, int locationId)
    {
        var result = new List<Appointment>();
        var firstAppointmentDateTime = startDate.AddHours(8);
        var lastAppointmentDateTime = endDate.AddDays(-1).AddHours(17);

        switch (scenerioId)
        {
            case 0:
                // Empty list
                break;
            case 1:
                // One appointment on the first day
                result.Add(new Appointment { Date = firstAppointmentDateTime, LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 2:
                // One appointment on the 5th day
                result.Add(new Appointment { Date = firstAppointmentDateTime.AddDays(5), LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 3:
                // One appointment on the last day
                result.Add(new Appointment { Date = lastAppointmentDateTime, LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 4:
                // Multiple appointments
                result.Add(new Appointment { Date = firstAppointmentDateTime, LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                result.Add(new Appointment { Date = firstAppointmentDateTime.AddDays(1), LocationId = locationId, Openings = 2, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                result.Add(new Appointment { Date = firstAppointmentDateTime.AddDays(2), LocationId = locationId, Openings = 3, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                result.Add(new Appointment { Date = lastAppointmentDateTime, LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                break;
            case 5:
                // A single appointment every day
                for (int i = 0; i < endDate.Subtract(startDate).Days; i++)
                {
                    result.Add(new Appointment { Date = firstAppointmentDateTime.AddDays(i), LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                }
                break;
            case 6:
                // Every other day has an appointment
                for (int i = 0; i < endDate.Subtract(startDate).Days; i++)
                {
                    if (i % 2 == 0)
                    {
                        result.Add(new Appointment { Date = firstAppointmentDateTime.AddDays(i), LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                    }
                }
                break;
            case 7:
                // Every day has a consecutive appointment every 10 minutes with an opening starting 8 am to 6 pm
                for (int i = 0; i < endDate.Subtract(startDate).Days; i++)
                {
                    var appointmentDateTime = firstAppointmentDateTime.AddDays(i);
                    while (appointmentDateTime.Hour < 18)
                    {
                        result.Add(new Appointment { Date = appointmentDateTime, LocationId = locationId, Openings = 1, TotalSlots = 3, Pending = 0, Conflicts = 0, Duration = 10 });
                        appointmentDateTime = appointmentDateTime.AddMinutes(10);
                    }
                }
                break;
            default:
                throw new ArgumentException("Invalid scenerioId");
        }

        return result;
    }

}
