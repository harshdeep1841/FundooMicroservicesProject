using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fundoo.User.Infrastructure.Security;

public class ServiceTokenService(
    IOptions<ServiceAuthOptions> options)
{
    private readonly ServiceAuthOptions _options = options.Value;

    public string CreateAccessToken()
    {
        Claim[] claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                _options.ClientId),

            new Claim(
                "client_id",
                _options.ClientId),

            new Claim(
                "scope",
                "notification.send")
        ];

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _options.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}