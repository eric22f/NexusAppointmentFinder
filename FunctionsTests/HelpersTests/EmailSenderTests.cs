using System.Configuration;
using Microsoft.Extensions.Configuration;
using NexusAzureFunctions.Helpers;
using Xunit;
namespace NexusAzureFunctionsTests.HelpersTests;

public class EmailSenderTests
{
    private readonly IConfiguration _config;
    private readonly EmailSender _emailSender;
    private readonly bool _enableUnitTests;
    private readonly string _toEmailAddress;
    private readonly string _toSmsEmailAdress;
    private readonly string _toFullName;

    public EmailSenderTests()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.test.settings.json", optional: true, reloadOnChange: true);
        _config = configurationBuilder.Build();

        string unitTestsEnabled = _config["Smtp:EnableEmailUnitTests"] ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:EnableEmailUnitTests'");
        _enableUnitTests = bool.Parse(unitTestsEnabled);
        _toEmailAddress = _config["Smtp:TestingToEmail"] ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:TestingToEmail'");
        _toFullName = _config["Smtp:TestingToFullName"] ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:TestingToFullName'");
        _toSmsEmailAdress = _config["Smtp:TestingToSmsEmail"] ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:TestingToSmsEmail'");

        _emailSender = new EmailSender(_config);
    }

    [Fact]
    public void SendEmail_ValidEmail_SendsEmail()
    {
        if (!_enableUnitTests)
        {
            return;
        }
        // Arrange
        string subject = "Test - Nexus interview available";
        string smsMsg =  $"{DateTime.Now:ddd M/d/yy h:mm tt}\nLocation (Location Full Description)";

        // Act
        _emailSender.SendPlainEmail(_toEmailAddress, _toFullName, subject, smsMsg);

        // Assert
        // No exception thrown
    }

    [Fact]
    public void SendSmsEmail_ValidEmail_SendsText()
    {
        if (!_enableUnitTests)
        {
            return;
        }
        // Arrange
        string subject = "Test - Nexus interview available";
        string smsMsg =  $"{DateTime.Now:ddd M/d/yy h:mm tt}\nLocation (Location Full Description)";

        // Act
        _emailSender.SendPlainEmail(_toSmsEmailAdress, _toFullName, subject, smsMsg);

        // Assert
        // No exception thrown
    }
}