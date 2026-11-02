namespace Fundoo.User.Application.Interfaces;

public interface INotificationClient
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}