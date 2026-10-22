using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMate.Api.Contracts;
using TaskMate.Api.Domain;
using TaskMate.Api.Services;

namespace TaskMate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskResponse>>> GetTasks(
        [FromQuery] TaskState? status,
        [FromQuery] TaskCategory? category,
        [FromQuery] TaskPriority? priority,
        [FromQuery] string? search,
        [FromQuery] DateOnly? dueFrom,
        [FromQuery] DateOnly? dueThrough,
        [FromQuery] bool excludeCompleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (search?.Length ?? 0) > 120 ||
            (dueFrom is not null && dueThrough is not null && dueFrom > dueThrough))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid task query.",
                Detail = "Page must be at least 1, pageSize must be between 1 and 100, search must be 120 characters or fewer, and dueFrom cannot be after dueThrough.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        var query = new TaskQuery(status, category, priority, search, dueFrom, dueThrough, excludeCompleted, page, pageSize);
        return Ok(await taskService.GetTasksAsync(userId.Value, query, cancellationToken));
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        var task = await taskService.GetTaskAsync(userId.Value, taskId, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        var task = await taskService.CreateTaskAsync(userId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { taskId = task.Id }, task);
    }

    [HttpPut("{taskId:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateTask(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        var task = await taskService.UpdateTaskAsync(userId.Value, taskId, request, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpDelete("{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid taskId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        return await taskService.DeleteTaskAsync(userId.Value, taskId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private Guid? GetUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}
