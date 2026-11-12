using System.Net.Http.Json;
using Fundoo.User.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Fundoo.User.Infrastructure.Services;

public sealed class NotificationClient(
    HttpClient httpClient,
    IConfiguration configuration) : INotificationClient
{
    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var apiKey =
            configuration["NotificationService:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Notification Service API key is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/notifications/email");

        request.Headers.Add("X-Service-Key", apiKey);

        request.Content = JsonContent.Create(new
        {
            To = to,
            Subject = subject,
            Body = body
        });

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);
        
        
        if (!response.IsSuccessStatusCode)
        {
            var responseBody =
                await response.Content.ReadAsStringAsync(
           
                    cancellationToken);

            throw new HttpRequestException(
                $"Notification Service returned " +
                $"{(int)response.StatusCode} ({response.StatusCode}). " +
                $"Response: {responseBody}",
                null,
                response.StatusCode);
        }
    }
}