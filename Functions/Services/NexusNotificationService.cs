using System.Diagnostics;
using System.Text;
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
    IConfiguration config, NexusDB nexusDB, EmailSender emailSender)
{
    private readonly ILogger<NexusNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Tracer _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    private readonly IConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly NexusDB _nexusDB = nexusDB ?? throw new ArgumentNullException(nameof(nexusDB));
    private readonly EmailSender _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));

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
                int maxShortNotifications = _config.GetValue<int>("Notifications:MaxShortNotificationsCount");
                maxShortNotifications = maxShortNotifications > 0 ? maxShortNotifications : 4;
                if (appointments.Count <= maxShortNotifications)
                {
                    _logger.LogInformation($"[{_tracer.Id}] Sending short SMS notifications for {appointments.Count} appointments for location id: {locationId}.");
                    SendShortListSmSNotifications(appointments, notificationList);
                }
                else
                {
                    _logger.LogInformation($"[{_tracer.Id}] Sending SMS notifications for all {appointments.Count} appointments.");
                    int maxTextLength = _config.GetValue<int>("Notifications:MaxTextLength");
                    SendAllAppointmentSmsNotifications(appointments, notificationList, maxTextLength);
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
    private void SendShortListSmSNotifications(List<Appointment> appointments, List<UserNotifications> notificationList)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        string plural = appointments.Count > 1 ? "s" : "";
        string smsSubject = $"Nexus interview{plural} found";
        string smsMsg = string.Join("\n", appointments.Select(a => a.Date.ToString("ddd M/d h:mm tt"))) + "\n";
        smsMsg += $"{notificationList.First().LocationName} ({notificationList.First().LocationDescription})".Trim();
        int smsMessageCount = 0;

        foreach (var user in notificationList)
        {
            // Send SMS notification
            if (SendSms(user.Phone, user.PhoneProviderId, smsSubject, smsMsg)) {
                smsMessageCount++;
                if (smsMessageCount % 25 == 0) {
                    Thread.Sleep(1000);
                }
            }
        }
        stopWatch.Stop();
        _logger.LogInformation($"[{_tracer.Id}] Sent SMS notifications for {appointments.Count} appointments to {notificationList.Count} users.");
        _logger.LogInformation($"[{_tracer.Id}] {smsMessageCount} SMS messages sent in {stopWatch.ElapsedMilliseconds} ms");
    }

    private void SendAllAppointmentSmsNotifications(List<Appointment> appointments, List<UserNotifications> notificationList
        , int maxTextLength)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var smsSubject = "Nexus interviews found";
        string location = $"{notificationList.First().LocationName} ({notificationList.First().LocationDescription})".Trim();
        var smsMsg = new StringBuilder();
        var groupedAppointments = appointments.GroupBy(a => a.Date.Date);
        maxTextLength = maxTextLength > 0 ? maxTextLength : 160;
        bool beginNewMessage = true;
        int count = 0;
        int smsMessageCount = 0;

        foreach (var group in groupedAppointments)
        {
            if (beginNewMessage) {
                smsMsg.Clear();
                smsMsg.AppendLine(location);
                beginNewMessage = false;
            }
            var earliestTime = group.Min(a => a.Date.TimeOfDay);
            string plural = group.Count() > 1 ? "s" : "";
            smsMsg.AppendLine($"{group.Key:ddd M/d}: {group.Count()} opening{plural} starting at {earliestTime:h:mm tt}");
            beginNewMessage = (smsMsg.Length > maxTextLength) || (count++ == groupedAppointments.Count() - 1);
            if (beginNewMessage)
            {
                foreach (var user in notificationList)
                {
                    if (SendSms(user.Phone, user.PhoneProviderId, smsSubject, smsMsg.ToString())) {
                        smsMessageCount++;
                        if (smsMessageCount % 25 == 0) {
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
        }
        stopWatch.Stop();
        _logger.LogInformation($"[{_tracer.Id}] Sent SMS notifications for all {appointments.Count} appointments to {notificationList.Count} users.");
        _logger.LogInformation($"[{_tracer.Id}] {smsMessageCount} SMS messages sent in {stopWatch.ElapsedMilliseconds} ms");
    }

    // Send SMS notification
    private bool SendSms(string? phone, int phoneProviderId, string smsSubject, string smsBody)
    {
        // Send SMS notification
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }
        string phoneEmail = GetPhoneEmail(phone, phoneProviderId);

        // Send the SMS notification
        _emailSender.SendPlainEmail(phoneEmail, string.Empty, smsSubject, smsBody);
        return true;
    }

    // Get the email address of the phone number to receive SMS from the phone provider
    private static string GetPhoneEmail(string phone, int phoneProviderId)
    {
        return phoneProviderId switch
        {
            // T-Mobile
            1 => phone + "@tmomail.net",
            _ => throw new Exception($"Phone provider id {phoneProviderId} is not supported."),
        };
    }
}