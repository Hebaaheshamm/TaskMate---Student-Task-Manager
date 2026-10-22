using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMate.Api.Contracts;
using TaskMate.Api.Services;

namespace TaskMate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(ITaskService taskService, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(
        [FromQuery] string? today,
        CancellationToken cancellationToken)
    {
        var userId = Guid.TryParse(User.FindFirst("sub")?.Value, out var parsedId) ? parsedId : (Guid?)null;
        if (userId is null)
        {
            return Unauthorized();
        }

        var localDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (!string.IsNullOrWhiteSpace(today) &&
            !DateOnly.TryParseExact(today, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out localDate))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid date.",
                Detail = "today must use the yyyy-MM-dd format.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(await taskService.GetDashboardAsync(userId.Value, localDate, cancellationToken));
    }
}
