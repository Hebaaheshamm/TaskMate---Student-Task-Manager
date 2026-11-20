using System.ComponentModel.DataAnnotations;

namespace TaskMate.Api.Contracts;

public sealed record RegisterRequest(
    [param: Required, StringLength(80, MinimumLength = 2)] string Name,
    [param: Required, EmailAddress, StringLength(254)] string Email,
    [param: Required, StringLength(128, MinimumLength = 6)] string Password);

public sealed record LoginRequest(
    [param: Required, EmailAddress, StringLength(254)] string Email,
    [param: Required, StringLength(128, MinimumLength = 1)] string Password);

public sealed record UserSummary(Guid Id, string Name, string Email);

public sealed record UpdateProfileRequest(
    [param: Required, StringLength(80, MinimumLength = 2)] string Name,
    [param: Required, EmailAddress, StringLength(254)] string Email);

public sealed record ChangePasswordRequest(
    [param: Required, StringLength(128, MinimumLength = 1)] string CurrentPassword,
    [param: Required, StringLength(128, MinimumLength = 6)] string NewPassword);

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserSummary User);
