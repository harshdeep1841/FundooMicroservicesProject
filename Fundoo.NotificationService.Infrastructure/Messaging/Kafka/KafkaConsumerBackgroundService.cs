
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fundoo.NotificationService.Infrastructure.Messaging.Kafka;

public sealed class KafkaConsumerBackgroundService(
    IOptions<KafkaOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaConsumerBackgroundService> logger)
    : BackgroundService
{
    private readonly KafkaOptions _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        using var consumer =
            new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(_options.PasswordResetTopic);

        logger.LogInformation(
            "Kafka consumer subscribed to topic {Topic}.",
            _options.PasswordResetTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);

                if (result is null)
                    continue;

                logger.LogInformation(
                    "Received Kafka event from partition {Partition}, offset {Offset}.",
                    result.Partition.Value,
                    result.Offset.Value);

                // Retry this message before consuming the next one.
                // This prevents a later offset from skipping a failed message.
                var retryDelay = TimeSpan.FromSeconds(1);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();

                        var eventConsumer = scope.ServiceProvider
                            .GetRequiredService<PasswordResetEventConsumer>();

                        await eventConsumer.ProcessAsync(
                            result.Message.Value,
                            stoppingToken);

                        // Commit only after email processing succeeds.
                        consumer.Commit(result);

                        logger.LogInformation(
                            "Kafka event successfully processed and committed.");

                        break;
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to process Kafka event at partition {Partition}, offset {Offset}. Retrying in {Delay} seconds.",
                            result.Partition.Value,
                            result.Offset.Value,
                            retryDelay.TotalSeconds);

                        await Task.Delay(retryDelay, stoppingToken);

                        retryDelay = TimeSpan.FromSeconds(
                            Math.Min(retryDelay.TotalSeconds * 2, 30));
                    }
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Kafka consumer is shutting down.");
        }
        finally
        {
            consumer.Close();
        }
    }
}
