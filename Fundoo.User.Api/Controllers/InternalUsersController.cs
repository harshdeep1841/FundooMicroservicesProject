using System.Security.Cryptography;
using System.Text;
using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.User.Api.Controllers;

[ApiController]
[Route("internal/users")]
public sealed class InternalUsersController(
    IAuthService authService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FindByEmail(
        [FromBody] InternalUserLookupRequest request,
        [FromHeader(Name = "X-Fundoo-Service-Key")] string? serviceKey,
        CancellationToken cancellationToken)
    {
        if (!IsValidServiceKey(serviceKey))
            return Unauthorized();

        var userId = await authService.FindUserIdByEmailAsync(
            request.Email,
            cancellationToken);

        return userId is null
            ? NotFound()
            : Ok(new { UserId = userId.Value });
    }

    [HttpGet("{userId:guid}/sessions/{sessionId:guid}/active")]
    public async Task<IActionResult> IsSessionActive(
        Guid userId,
        Guid sessionId,
        [FromHeader(Name = "X-Fundoo-Service-Key")] string? serviceKey,
        CancellationToken cancellationToken)
    {
        if (!IsValidServiceKey(serviceKey))
            return Unauthorized();

        var isActive = await authService.IsSessionActiveAsync(
            userId, sessionId, cancellationToken);
        return Ok(new { IsActive = isActive });
    }

    private bool IsValidServiceKey(string? providedKey)
    {
        var expectedKey = configuration["UserDirectory:ServiceKey"];
        return !string.IsNullOrWhiteSpace(expectedKey) &&
               !string.IsNullOrEmpty(providedKey) &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(providedKey),
                   Encoding.UTF8.GetBytes(expectedKey));
    }
}
