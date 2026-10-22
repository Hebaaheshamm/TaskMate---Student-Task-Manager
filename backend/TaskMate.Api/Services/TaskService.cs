using TaskMate.Api.Contracts;
using TaskMate.Api.Domain;
using TaskMate.Api.Repositories;

namespace TaskMate.Api.Services;

public sealed class TaskService(ITaskRepository tasks, TimeProvider timeProvider) : ITaskService
{
    public async Task<PagedResult<TaskResponse>> GetTasksAsync(Guid userId, TaskQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await tasks.FindPageAsync(userId, query, cancellationToken);
        var responses = items.Select(ToResponse).ToArray();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / query.PageSize);
        return new PagedResult<TaskResponse>(responses, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<TaskResponse?> GetTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.FindByIdAsync(userId, taskId, cancellationToken);
        return task is null ? null : ToResponse(task);
    }

    public async Task<TaskResponse> CreateTaskAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var task = new TaskItem
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description),
            DueDate = request.DueDate,
            Priority = request.Priority,
            Status = request.Status,
            Category = request.Category,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await tasks.AddAsync(task, cancellationToken);
        return ToResponse(task);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await tasks.FindByIdAsync(userId, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.Title = request.Title.Trim();
        task.Description = NormalizeDescription(request.Description);
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;
        task.Status = request.Status;
        task.Category = request.Category;
        task.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await tasks.SaveChangesAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.FindByIdAsync(userId, taskId, cancellationToken);
        if (task is null)
        {
            return false;
        }
        await tasks.DeleteAsync(task, cancellationToken);
        return true;
    }

    public Task<DashboardResponse> GetDashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken) =>
        tasks.GetDashboardAsync(userId, today, cancellationToken);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static TaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.DueDate,
        task.Priority,
        task.Status,
        task.Category,
        task.CreatedAtUtc,
        task.UpdatedAtUtc);
}
