using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NexusAzureFunctions.Services;
using NexusAzureFunctions.Helpers;

namespace NexusAzureFunctions;

public class FunctionHttpGetAppointments(ILogger<FunctionHttpGetAppointments> logger, NexusAppointmentService appointmentsSvc,
    Tracer tracer)
{
    private readonly ILogger<FunctionHttpGetAppointments> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NexusAppointmentService _appointmentsSvc = appointmentsSvc ?? throw new ArgumentNullException(nameof(appointmentsSvc));
    private readonly Tracer _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));

    [Function("FunctionHttpGetAppointments")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        FunctionContext context)
    {
        try
        {
            _logger.LogInformation($"[{_tracer.Id}]{context.FunctionDefinition.Name} requested with Invocation ID: {context.InvocationId}.");
            var availableAppointments = await _appointmentsSvc.ProcessAppointments();
        
            if (!_appointmentsSvc.IsProcessAppointmentsSuccess)
            { 
                return new BadRequestObjectResult($"An error occurred processing appointments. (Reference trace log id: {_tracer.Id})");
            }
            if (availableAppointments.Count == 0)
            {
                return new OkObjectResult($"No appointments available.");
            }
            if (availableAppointments.Count == 1)
            {
                return new OkObjectResult($"One Appointment available: {string.Join("\n", availableAppointments.Select(a => a.ToString()))}");
            }
            return new OkObjectResult($"Appointments available: {availableAppointments.Count}.\n{string.Join("\n", availableAppointments.Select(a => a.ToString()))}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{_tracer.Id}]{context.FunctionDefinition.Name} failed with Invocation ID: {context.InvocationId}.");
            return new BadRequestObjectResult($"An error occurred processing appointments. (Reference trace log id: {_tracer.Id})");
        }
    }
}