using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NexusAzureFunctions.Helpers;
using NexusAzureFunctions.Models;

namespace NexusAzureFunctions.Services;

// This service is responsible for processing messages from the Service Bus
// and sending notifications to users assigned to the appointment location received
public class NexusNotificationService(ILogger<NexusNotificationService> logger, Tracer tracer,
    IConfiguration config, NexusDB nexusDB)
{
    private readonly ILogger<NexusNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Tracer _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly NexusDB _nexusDB = nexusDB ?? throw new ArgumentNullException(nameof(nexusDB));

    public async Task ProcessMessageAsync(ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        try
        {
            // Process the message
            _logger.LogInformation($"[{_tracer.Id}] Processing message: {message.MessageId}");

            // Get appointments from the message body
            var messageBody = message.Body.ToString();
            var appointments = JsonConvert.DeserializeObject<List<Appointment>>(messageBody) ?? [];
            if (appointments.Count == 0)
            {
                _logger.LogWarning($"[{_tracer.Id}] No appointments found in the message body.");
                // Complete the message
                await messageActions.CompleteMessageAsync(message);
                return;
            }
            _logger.LogInformation($"[{_tracer.Id}] {appointments.Count} Appointments found from Message body: {messageBody}");

            // Get the location id from the appointments
            var locationId = appointments.First().LocationId;

            // Get the list of everyone to be notified for this location
            var notificationList = GetUserNotificationList(locationId);
            if (notificationList.Count == 0)
            {
                // Complete the message
                await messageActions.CompleteMessageAsync(message);
                return;
            }

            // Get users with confirmed contact information
            var smsList = notificationList.Where(u => u.PhoneConfirmed).ToList();
            var emailList = notificationList.Where(u => u.EmailConfirmed || u.AlternateEmailConfirmed).ToList();
            if (smsList.Count == 0 && emailList.Count == 0)
            {
                _logger.LogWarning($"[{_tracer.Id}] No users with confirmed contact information found for LocationId: {locationId}");
                // Complete the message
                await messageActions.CompleteMessageAsync(message);
                return;
            }

            if (smsList.Count > 0)
            {
                _logger.LogInformation($"[{_tracer.Id}] Sending SMS notifications to {smsList.Count} users for location id: {locationId}.");

                // Get the maximum number of short notifications to send
                // short notifications include the actual appointment date and time
                // long notifications include the dates and the number of appointments on that date
                int maxShortNotifications = _config.GetValue<int>("MaxShortNotificationsCount");
                if (appointments.Count <= maxShortNotifications)
                {
                    _logger.LogInformation($"[{_tracer.Id}] Sending short SMS notifications for {appointments.Count} appointments for location id: {locationId}.");
                    SendShortListSmSNotifications(appointments, notificationList);
                }
                else
                {
                    _logger.LogInformation($"[{_tracer.Id}] Sending SMS notifications for all {appointments.Count} appointments.");
                    SendAllAppointmentSmsNotifications(appointments, notificationList);
                }
            }

            // Get the dates of all appointments
            var appointmentDates = appointments.Select(a => a.Date.Date).ToList();

            // Get the time slot for the first appointment
            var timeSlot = appointments.First().Date.TimeOfDay;


            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{_tracer.Id}] An error occurred while processing Service Bus message id: {message.MessageId}");
            // Dead-letter the message
            await messageActions.DeadLetterMessageAsync(message);
        }
    }

    // Get the list of users assigned to this location
    private List<UserNotifications> GetUserNotificationList(int locationId)
    {
        // Get the list of users assigned to this location
        var users = _nexusDB.GetUsersAssignedToLocation(locationId);

        // Filter any users that do not have confirmed contact information
        users = users.Where(u => u.EmailConfirmed || u.AlternateEmailConfirmed || u.PhoneConfirmed).ToList();
        if (users.Count == 0)
        {
            _logger.LogWarning($"[{_tracer.Id}] No active users found for LocationId: {locationId}");
        }
        return users;
    }

    // Send short SMS notifications to users for a small list of appointments
    private static void SendShortListSmSNotifications(List<Appointment> appointments, List<UserNotifications> notificationList)
    {
        string smsMsg = "Nexus interview found:\n";
        smsMsg += string.Join("\n", appointments.Select(a => a.Date.ToString("ddd M/d/yy h:mm tt")));
        smsMsg += "\n{appointments.First().LocationName} ({appointments.First().LocationDescription})";

        foreach (var user in notificationList)
        {
            // Send SMS notification
            SendSms(user.Phone, smsMsg);
        }
    }

    private static void SendAllAppointmentSmsNotifications(List<Appointment> appointments, List<UserNotifications> notificationList)
    {

    }

    // Send SMS notification
    private static void SendSms(string? phone, string smsMsg)
    {
        // Send SMS notification
        
    }
}