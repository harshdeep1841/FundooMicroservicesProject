namespace Fundoo.User.Infrastructure.Security;

public class ServiceAuthOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 5;
}