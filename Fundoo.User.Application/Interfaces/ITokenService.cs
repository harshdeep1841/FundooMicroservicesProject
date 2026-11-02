using Fundoo.User.Domain.Entities;

namespace Fundoo.User.Application.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(AppUser user);
}