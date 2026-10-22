using TaskMate.Api.Contracts;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Repositories;

public interface ITaskRepository
{
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> FindPageAsync(Guid userId, TaskQuery query, CancellationToken cancellationToken);
    Task<TaskItem?> FindByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
    Task<DashboardResponse> GetDashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken);
}
