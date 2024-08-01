using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using System.Drawing.Text;
namespace NexusAzureFunctions.Helpers;

public class EmailSender
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpFromDescription;
    private readonly string _smtpPassword;
    private readonly bool _isEnabled;

    // Send Emails using MailKit
    public EmailSender(IConfiguration config)
    {
        _isEnabled = config.GetValue<bool>("Smtp:Enabled");
        if (_isEnabled)
        {
            _smtpServer = config["Smtp:Server"] 
                ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:Server'.  Last known good value 'lx01.aspnix.com'");
            _smtpPort = config.GetValue<int>("Smtp:Port");
            if (_smtpPort == 0)
            {
                throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:Port'. Last known good value '26'");
            }
            _smtpUsername = config["Smtp:User"] 
                ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:User'. Last known good value 'nexus@funforallsoftware.com'");
            _smtpPassword = config["Smtp:Password"] 
                ?? throw new ConfigurationErrorsException("Missing configuration setting for 'Smtp:Password'");
            _smtpFromDescription = config["Smtp:FromDescription"] ?? "Nexus Notification";
        }
        else
        {
            _smtpServer = string.Empty;
            _smtpPort = 0;
            _smtpUsername = string.Empty;
            _smtpPassword = string.Empty;
            _smtpFromDescription = string.Empty;
        }
    }
    
    // Send a plain text email
    public void SendPlainEmail(string toEmail, string toFullName, string subject, string body)
    {
        if (!_isEnabled)
        {
            return;
        }
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_smtpFromDescription, _smtpUsername));
        msg.To.Add(new MailboxAddress(toFullName, toEmail));
        msg.Subject = subject;

        msg.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        client.Connect(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
        client.Authenticate(_smtpUsername, _smtpPassword);

        client.Send(msg);
        client.Disconnect(true);
    }

    // Must be set to true in order to send emails
    public bool IsEnabled
    {
        get { return _isEnabled; }
        private set { throw new NotImplementedException("IsEnabled can only be set in the constructor."); }
    }   
}
