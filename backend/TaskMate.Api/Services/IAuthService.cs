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
}
