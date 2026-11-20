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
        if (result.Response is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = result.FailureReason switch
                {
                    LoginFailureReason.AccountNotFound => "No account was found with this email. Please create an account first.",
                    LoginFailureReason.IncorrectPassword => "The password is incorrect. Please try again.",
                    _ => "We couldn't sign you in. Please try again."
                },
                Status = StatusCodes.Status401Unauthorized
            });
        }
        return Ok(result.Response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserSummary>> Me(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType<UserSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserSummary>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
        var user = await authService.UpdateProfileAsync(userId, request, cancellationToken);
        return user is null
            ? Conflict(new ProblemDetails { Title = "This email address is already in use.", Status = StatusCodes.Status409Conflict })
            : Ok(user);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
        return await authService.ChangePasswordAsync(userId, request, cancellationToken)
            ? NoContent()
            : BadRequest(new ProblemDetails { Title = "Your current password is incorrect.", Status = StatusCodes.Status400BadRequest });
    }
}
