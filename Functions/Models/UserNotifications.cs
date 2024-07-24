using System.Diagnostics;

namespace NexusAzureFunctions.Models;

// Nexus user Appointment Notifications result
[DebuggerDisplay("UserNotifications: {ToString()}")]
public class UserNotifications
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? AlternateEmail { get; set; }
    public string? Phone { get; set; }
    public int PhoneProviderId { get; set; }
    public bool NotifyByEmail { get; set; }
    public bool NotifyBySms { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool AlternateEmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? LocationDescription { get; set; }

    public override string ToString()
    {
        return $"UserId: {UserId}, Email: {Email}, AlternateEmail: {AlternateEmail}, Phone: {Phone}, NotifyByEmail: {NotifyByEmail}, NotifyBySms: {NotifyBySms}, EmailConfirmed: {EmailConfirmed}, AlternateEmailConfirmed: {AlternateEmailConfirmed}, PhoneConfirmed: {PhoneConfirmed}, LocationId: {LocationId}, LocationName: {LocationName}";
    }
}