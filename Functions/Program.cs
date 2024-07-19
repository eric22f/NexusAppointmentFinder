using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NexusAzureFunctions.Services;
using NexusAzureFunctions.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddEnvironmentVariables();
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Configure logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddApplicationInsights(); // Optional
            logging.AddConfiguration(configuration.GetSection("Logging"));
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add services
        services.AddTransient<NexusAppointmentService>();
        services.AddScoped<Tracer>();

        // Add Configuration
        services.AddSingleton<IConfiguration>(sp => context.Configuration);

        // Register Redis connection multiplexer
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            string redisConnectionString = configuration["RedisConnectionString"] ?? throw new ConfigurationErrorsException("Configuration setting 'RedisConnectionString' not found.");
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });

        // Register AppointmentCacheRedis service
        services.AddSingleton<AppointmentCacheRedis>();

        // Register AppointmentCacheFactory as a singleton
        services.AddSingleton<AppointmentCacheFactory>();
    })
    .Build();

host.Run();
