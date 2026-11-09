namespace Fundoo.User.Api.Contracts;

public class LoginResponseV2
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;
}