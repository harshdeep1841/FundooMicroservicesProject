using System.Security.Cryptography;
using System.Text;

namespace Fundoo.NotificationService.Api.Middleware;

public class ServiceKeyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IConfiguration configuration)
    {
        // Protect the email-sending API.
        if (!context.Request.Path.StartsWithSegments(
                "/api/notifications"))
        {
            await next(context);
            return;
        }

        var configuredKey =
            configuration["ServiceAuthentication:ApiKey"];

        var suppliedKey =
            context.Request.Headers["X-Service-Key"].ToString();

        if (string.IsNullOrWhiteSpace(configuredKey) ||
            string.IsNullOrWhiteSpace(suppliedKey))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(
                new { message = "Unauthorized service." });

            return;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(configuredKey);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey);

        if (!CryptographicOperations.FixedTimeEquals(
                expectedBytes, suppliedBytes))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(
                new { message = "Unauthorized service." });

            return;
        }

        await next(context);
    }
}