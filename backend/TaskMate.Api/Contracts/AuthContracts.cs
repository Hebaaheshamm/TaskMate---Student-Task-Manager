using System.ComponentModel.DataAnnotations;

namespace TaskMate.Api.Contracts;

public sealed record RegisterRequest(
    [param: Required, StringLength(80, MinimumLength = 2)] string Name,
    [param: Required, EmailAddress, StringLength(254)] string Email,
    [param: Required, StringLength(128, MinimumLength = 12)] string Password);

public sealed record LoginRequest(
    [param: Required, EmailAddress, StringLength(254)] string Email,
    [param: Required, StringLength(128, MinimumLength = 1)] string Password);

public sealed record UserSummary(Guid Id, string Name, string Email);

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserSummary User);
