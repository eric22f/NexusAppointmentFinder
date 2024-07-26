using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexusAzureFunctions
{
    public class TimerTriggerGetNexusAppointments(ILoggerFactory loggerFactory, IConfiguration config)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<TimerTriggerGetNexusAppointments>();
        private readonly IConfiguration _config = config;

        [Function("TimerTriggerGetNexusAppointments")]
        // Runs every 2 minutes between 7am and 9pm
        public async Task Run([TimerTrigger("0 */2 7-21 * * *")] TimerInfo myTimer)
        {
            // Timer Trigger can be ignored to help during testing
            if (_config["LocalDebugging:IgnoreTimerFunctions"] == "true")
            {
                _logger.LogInformation("LocalDebugging:IgnoreTimerFunctions is true.  Exiting TimerTriggerGetNexusAppointments.");
                return;
            }
            _logger.LogInformation($"Timer trigger TimerTriggerGetNexusAppointments executed at: {DateTime.Now}");
            if (myTimer.IsPastDue)
            {
                _logger.LogInformation("TimerTriggerGetNexusAppointments Timer is running late!");
            }
            
            // Call Function GetNexusAppointments using an HTTP request
            using var client = new HttpClient();
            var httpSetting = Environment.GetEnvironmentVariable("WEBSITE_HTTPS") + "" != "on" ? "http" : "https";
            var hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
            var functionUrl = $"{httpSetting}://{hostname}/api/GetNexusAppointments";
            var response = await client.GetAsync(functionUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Response from GetNexusAppointments: {responseContent}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
