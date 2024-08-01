using System.Configuration;
using Microsoft.Extensions.Configuration;
using NexusAzureFunctions.Helpers;
using Xunit;
using Xunit.Abstractions;
namespace NexusAzureFunctionsTests.HelpersTests;

public class EmailSenderTests
{
    private readonly ITestOutputHelper _output;
    private readonly IConfiguration _config;
    private readonly EmailSender _emailSender;
    private readonly bool _enableUnitTests;
    private readonly string _toEmailAddress;
    private readonly string _toSmsEmailAdress;
    private readonly string _toFullName;

    public EmailSenderTests(ITestOutputHelper output)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("local.test.settings.json", optional: true, reloadOnChange: true);
        _config = configurationBuilder.Build();

        string unitTestsEnabled = _config["TestFlags:EnableEmailTests"] ?? throw new ConfigurationErrorsException("Missing configuration setting for TestFlags:EnableEmailTests'");
        _enableUnitTests = bool.Parse(unitTestsEnabled);
        _toEmailAddress = _config["Smtp:TestingToEmail"] ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:TestingToEmail'");
        _toFullName = _config["Smtp:TestingToFullName"] ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:TestingToFullName'");
        _toSmsEmailAdress = _config["Smtp:TestingToSmsEmail"] ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:TestingToSmsEmail'");

        _emailSender = new EmailSender(_config);
        _output = output;
    }

    [Fact]
    public void SendEmail_ValidEmail_SendsEmail()
    {
        if (!_enableUnitTests)
        {
            _output.WriteLine("Skipping test. Enable Email tests in local.test.settings.json to run this test.");
            return;
        }
        if (!_emailSender.IsEnabled) {
            throw new ConfigurationErrorsException("Smtp is not enabled. Check configuration settings in 'local.settings.json'.");
        }
        // Arrange
        string subject = "Test - Nexus interview available";
        string smsMsg =  $"Nexus interview available (Test):\n{DateTime.Now:ddd M/d/yy h:mm tt}\nLocation (Location Full Description)";

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
            _output.WriteLine("Skipping test. Enable Email tests in local.test.settings.json to run this test.");
            return;
        }
        if (!_emailSender.IsEnabled) {
            throw new ConfigurationErrorsException("Smtp is not enabled. Check configuration settings in 'local.settings.json'.");
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