using TaskMate.Api.Domain;

namespace TaskMate.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user);
}
