using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Functions.Services;
using Functions.Helpers;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add services
        services.AddTransient<NexusAppointmentService>();
        services.AddTransient<Tracer>();

        // Add Logger
        services.AddLogging();

        // Add Configuration
        services.AddSingleton<IConfiguration>(sp => context.Configuration);

        // Register AppointmentCacheRedis service
        services.AddSingleton<AppointmentCacheRedis>();

        // Register AppointmentCacheFactory as a singleton
        services.AddSingleton<AppointmentCacheFactory>();
    })
    .Build();

host.Run();
