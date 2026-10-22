using System.ComponentModel.DataAnnotations;

namespace TaskMate.Api.Contracts;

public sealed record RegisterRequest(
    [property: Required, StringLength(80, MinimumLength = 2)] string Name,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(128, MinimumLength = 12)] string Password);

public sealed record LoginRequest(
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required, StringLength(128, MinimumLength = 1)] string Password);

public sealed record UserSummary(Guid Id, string Name, string Email);

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserSummary User);
