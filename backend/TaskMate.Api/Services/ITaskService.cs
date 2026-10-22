using TaskMate.Api.Contracts;

namespace TaskMate.Api.Services;

public interface ITaskService
{
    Task<PagedResult<TaskResponse>> GetTasksAsync(Guid userId, TaskQuery query, CancellationToken cancellationToken);
    Task<TaskResponse?> GetTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);
    Task<TaskResponse> CreateTaskAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken);
    Task<TaskResponse?> UpdateTaskAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);
    Task<DashboardResponse> GetDashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken);
}
