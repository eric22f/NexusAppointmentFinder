using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Functions.Services;
using Functions.Helpers;

namespace Functions
{
    public class HttpGetAppointmentsFunction
    {
        private readonly ILogger<HttpGetAppointmentsFunction> _logger;
        private readonly NexusAppointmentService _appointmentsSvc;
        public HttpGetAppointmentsFunction(ILogger<HttpGetAppointmentsFunction> logger, NexusAppointmentService appointmentsSvc)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appointmentsSvc = appointmentsSvc ?? throw new ArgumentNullException(nameof(appointmentsSvc));
        }

        [Function("HttpGetAppointmentsFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation($"[{Tracer.Id}]HttpGetAppointmentsFunction requested.");
            int result = await _appointmentsSvc.ProcessAppointments();
        
            return result == -1 ? new BadRequestObjectResult($"An error occurred processing appointments. (Reference trace log id: {Tracer.Id})") 
                : new OkObjectResult($"Processed {result} appointments");
        }
    }
}