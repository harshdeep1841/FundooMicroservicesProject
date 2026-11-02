using Fundoo.NotificationService.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Fundoo.NotificationService.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var settings = configuration.GetSection("Email");

        var host = settings["Host"];

        if (!int.TryParse(settings["Port"], out var port))
        {
            throw new InvalidOperationException(
                "Email SMTP port is invalid.");
        }

        var senderName = settings["SenderName"];
        var senderEmail = settings["SenderEmail"];
        var username = settings["Username"];
        var password = settings["Password"];

        if (!MailboxAddress.TryParse(to, out var recipient))
        {
            throw new ArgumentException(
                "Invalid recipient email address.",
                nameof(to));
        }

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(senderName, senderEmail));

        message.To.Add(recipient);
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient
        {
            Timeout = 15000
        };

        try
        {
            client.CheckCertificateRevocation = false;

            await client.ConnectAsync(
                host,
                port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await client.AuthenticateAsync(
                username,
                password,
                cancellationToken);

            await client.SendAsync(
                message,
                cancellationToken);

            logger.LogInformation(
                "Email sent successfully to {Recipient}.",
                to);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send email through SMTP.");

            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    true,
                    CancellationToken.None);
            }
        }
    }
}