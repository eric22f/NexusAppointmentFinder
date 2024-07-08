using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions
{
    public class HttpGetAppointmentsFunction
    {
        private readonly ILogger<HttpGetAppointmentsFunction> _logger;

        public HttpGetAppointmentsFunction(ILogger<HttpGetAppointmentsFunction> logger)
        {
            _logger = logger;
        }

        [Function("HttpGetAppointmentsFunction")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
