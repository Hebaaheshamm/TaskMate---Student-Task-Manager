using TaskMate.Api.Contracts;

namespace TaskMate.Api.Services;

public enum LoginFailureReason
{
    AccountNotFound,
    IncorrectPassword
}

public sealed record LoginResult(AuthResponse? Response, LoginFailureReason? FailureReason);

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserSummary?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserSummary?> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}
