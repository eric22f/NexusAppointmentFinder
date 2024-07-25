using System.Diagnostics;

namespace NexusAzureFunctions.Models;

// Nexus user Appointment Notifications result
[DebuggerDisplay("UserNotifications: {ToString()}")]
public class UserNotifications
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public string? AlternateEmail { get; set; }
    public string? Phone { get; set; }
    public int PhoneProviderId { get; set; }
    public bool NotifyByEmail { get; set; }
    public bool NotifyBySms { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool AlternateEmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
    public int LocationId { get; set; }
    public required string LocationName { get; set; }
    public string? LocationDescription { get; set; }

    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }

    public override string ToString()
    {
        return $"UserId: {UserId}, Email: {Email}, AlternateEmail: {AlternateEmail}, Phone: {Phone}, PhoneProviderId: {PhoneProviderId}, NotifyByEmail: {NotifyByEmail}, NotifyBySms: {NotifyBySms}, FirstName: {FirstName}, LastName: {LastName}, EmailConfirmed: {EmailConfirmed}, AlternateEmailConfirmed: {AlternateEmailConfirmed}, PhoneConfirmed: {PhoneConfirmed}, LocationId: {LocationId}, LocationName: {LocationName}, LocationDescription: {LocationDescription}";
    }
}