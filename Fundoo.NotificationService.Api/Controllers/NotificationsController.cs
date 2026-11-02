using Fundoo.NotificationService.Api.Contract;
using Fundoo.NotificationService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.NotificationService.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(
    IEmailSender emailSender,
    ILogger<NotificationsController> logger) : ControllerBase
{
    [HttpPost("email")]
    public async Task<IActionResult> SendEmail(
        [FromBody] SendEmailRequest request,
        CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() =>
        {
            logger.LogWarning(
                "Notification API: Request cancellation detected.");
        });

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken);

            await emailSender.SendEmailAsync(
                request.To,
                request.Subject,
                request.Body,
                cancellationToken);

            return Ok(new
            {
                message = "Email sent successfully."
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Notification API: Request cancelled.");

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Notification email operation failed.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Unable to send email."
                });
        }
    }
}