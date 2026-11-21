using System.Text.Json;
using Fundoo.NotificationService.Application.Events;
using Fundoo.NotificationService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fundoo.NotificationService.Infrastructure.Messaging.Kafka;

public sealed class PasswordResetEventConsumer(
    IEmailSender emailSender,
    ILogger<PasswordResetEventConsumer> logger)
{
    public async Task ProcessAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var resetEvent = JsonSerializer.Deserialize<PasswordResetRequestedEvent>(
            message,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        
        if (resetEvent is null)
        {
            throw new InvalidOperationException(
                "Unable to deserialize password-reset event.");
        }

        if (string.IsNullOrWhiteSpace(resetEvent.Email) ||
            string.IsNullOrWhiteSpace(resetEvent.ResetToken))
        {
            throw new InvalidOperationException(
                "Password-reset event contains invalid data.");
        }

        var body =
            $"Your password-reset token is: {resetEvent.ResetToken}\n\n" +
            $"It expires at {resetEvent.ExpiresAtUtc:u} UTC and can only be used once.";

        await emailSender.SendEmailAsync(
            resetEvent.Email,
            "Reset your Fundoo password",
            body,
            cancellationToken);

        logger.LogInformation(
            "Password-reset email successfully processed for event {EventId}.",
            resetEvent.EventId);
    }
}