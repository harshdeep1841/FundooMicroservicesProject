using Fundoo.User.Application.Events;

namespace Fundoo.User.Application.Interfaces;

public interface IPasswordResetEventPublisher
{
    Task PublishAsync(
        PasswordResetRequestedEvent message,
        CancellationToken cancellationToken = default
    );
}