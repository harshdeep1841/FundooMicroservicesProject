namespace Fundoo.NotificationService.Infrastructure.Messaging.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string PasswordResetTopic { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;
}