using System.Text.Json;
using Confluent.Kafka;
using Fundoo.User.Application.Events;
using Fundoo.User.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fundoo.User.Infrastructure.Messaging.Kafka;

public sealed class KafkaPasswordResetEventPublisher(
    IProducer<string, string> producer,
    IOptions<KafkaOptions> options,
    ILogger<KafkaPasswordResetEventPublisher> logger)
    : IPasswordResetEventPublisher
{
    private readonly KafkaOptions _options = options.Value;

    public async Task PublishAsync(
        PasswordResetRequestedEvent message,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);

        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = json
        };

        var deliveryResult = await producer.ProduceAsync(
            _options.PasswordResetTopic,
            kafkaMessage,
            cancellationToken);

        logger.LogInformation(
            "Password-reset event {EventId} published to Kafka partition {Partition}.",
            message.EventId,
            deliveryResult.Partition.Value);
    }
}