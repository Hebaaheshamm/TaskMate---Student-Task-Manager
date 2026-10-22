using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskMate.Api.Contracts;
using TaskMate.Api.Services;

namespace TaskMate.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        if (result is null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "An account already exists for this email address.",
                Status = StatusCodes.Status409Conflict
            });
        }
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "The email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserSummary>(StatusCodes.Status200OK)]
    public ActionResult<UserSummary> Me()
    {
        var id = User.FindFirstValue("sub");
        var name = User.FindFirstValue("name");
        var email = User.FindFirstValue("email");
        if (!Guid.TryParse(id, out var userId) || name is null || email is null)
        {
            return Unauthorized();
        }
        return Ok(new UserSummary(userId, name, email));
    }
}
