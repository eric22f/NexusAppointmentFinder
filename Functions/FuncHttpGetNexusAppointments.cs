using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NexusAzureFunctions.Services;
using NexusAzureFunctions.Helpers;

namespace NexusAzureFunctions;

public class FuncHttpGetNexusAppointments(ILogger<FuncHttpGetNexusAppointments> logger, NexusAppointmentService appointmentsSvc,
    Tracer tracer)
{
    private readonly ILogger<FuncHttpGetNexusAppointments> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly NexusAppointmentService _appointmentsSvc = appointmentsSvc ?? throw new ArgumentNullException(nameof(appointmentsSvc));
    private readonly Tracer _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));

    [Function("GetNexusAppointments")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        FunctionContext context)
    {
        try
        {
            _logger.LogInformation($"[{_tracer.Id}] {context.FunctionDefinition.Name} requested with Invocation ID: {context.InvocationId}.");
            var availableAppointments = await _appointmentsSvc.ProcessAppointments();
            int days = _appointmentsSvc.TotalDays;
            int count = availableAppointments.Count;
        
            if (!_appointmentsSvc.IsProcessAppointmentsSuccess)
            { 
                return new BadRequestObjectResult($"An error occurred processing appointments. (Reference trace log id: {_tracer.Id})");
            }
            string plural = count == 1 ? "" : "s";
            string display = count == 0 ? "" : $"\n{string.Join("\n", availableAppointments.Select(a => a.ToString()))}";
            string number = NumbersToWords.ConvertToWords(count, true);
            return new OkObjectResult($"{number} appointment{plural} found within the next {days} days as of {DateTime.Now:M-d-yyyy h:mm:ss tt}.{display}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{_tracer.Id}] {context.FunctionDefinition.Name} failed with Invocation ID: {context.InvocationId}.");
            return new BadRequestObjectResult($"An error occurred processing appointments. (Reference trace log id: {_tracer.Id})");
        }
    }
}