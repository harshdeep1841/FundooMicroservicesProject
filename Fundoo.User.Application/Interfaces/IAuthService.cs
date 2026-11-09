using Fundoo.User.Domain.Entities;

namespace Fundoo.User.Application.Interfaces;

public interface IAuthService
{
    Task<AppUser> RegisterAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken);

    Task<string?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
    
    
    Task ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken);
    
    Task<bool> ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken);

    public Task<string?> GoogleLoginAsync(
        string googleUserId,
        string email,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken);

}