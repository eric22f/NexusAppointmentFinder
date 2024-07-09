using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Functions.Services;
using Functions.Helpers;

namespace Functions
{
    public class HttpGetAppointmentsFunction(ILogger<HttpGetAppointmentsFunction> logger, NexusAppointmentService appointmentsSvc,
        Tracer tracer)
    {
        private readonly ILogger<HttpGetAppointmentsFunction> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly NexusAppointmentService _appointmentsSvc = appointmentsSvc ?? throw new ArgumentNullException(nameof(appointmentsSvc));
        private readonly Tracer _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));

        [Function("HttpGetAppointmentsFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            FunctionContext context)
        {
            _logger.LogInformation($"[{_tracer.Id}]{context.FunctionDefinition.Name} requested with Invocation ID: {context.InvocationId}.");
            int result = await _appointmentsSvc.ProcessAppointments();
        
            return result == -1 ? new BadRequestObjectResult($"An error occurred processing appointments. (Reference trace log id: {_tracer.Id})") 
                : new OkObjectResult($"Processed {result} appointments");
        }
    }
}