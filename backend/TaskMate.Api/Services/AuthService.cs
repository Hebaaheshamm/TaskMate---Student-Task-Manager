using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Domain;
using TaskMate.Api.Repositories;

namespace TaskMate.Api.Services;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    private const string DummyHash = "v1.600000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return null;
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        try
        {
            await users.AddAsync(user, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index is authoritative when two registrations race.
            if (await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
            {
                return null;
            }
            throw;
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByNormalizedEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        var hash = user?.PasswordHash ?? DummyHash;
        if (!passwordHasher.Verify(request.Password, hash) || user is null)
        {
            return null;
        }

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAtUtc) = tokenService.CreateAccessToken(user);
        return new AuthResponse(token, "Bearer", expiresAtUtc, new UserSummary(user.Id, user.Name, user.Email));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
