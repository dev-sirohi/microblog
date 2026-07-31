using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Microblog.Api.Infrastructure.Observability;

namespace Microblog.Api.Infrastructure.Messaging.AzureServiceBus;

/// <summary>
/// Hosted service that consumes events from Azure Service Bus.
/// Activated when <c>Features:MessagingProvider = "azure-service-bus"</c>.
/// </summary>
public sealed class ServiceBusConsumerService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<ServiceBusConsumerService> _logger;
    private ServiceBusProcessor? _processor;

    public ServiceBusConsumerService(IServiceProvider services, IConfiguration config, ILogger<ServiceBusConsumerService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServiceBusConsumerService started");

        string connectionString = _config["Azure:ServiceBusConnectionString"] ?? string.Empty;
        string queueName = _config["Azure:ServiceBusQueueName"] ?? "microblog-events";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Azure Service Bus connection string not set; consumer inactive");
            return;
        }

        var client = new ServiceBusClient(connectionString);
        _processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false,
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }

        await _processor.StopProcessingAsync();
        await _processor.DisposeAsync();
        await client.DisposeAsync();
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            string topic = args.Message.Subject ?? "unknown";
            AppMetrics.BackgroundSyncProcessed.Inc();
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            AppMetrics.BackgroundSyncErrors.Inc();
            _logger.LogError(ex, "Service Bus message processing failed");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor error on {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }
}
