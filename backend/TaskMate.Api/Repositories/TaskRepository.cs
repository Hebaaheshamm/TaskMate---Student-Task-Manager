using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Repositories;

public sealed class TaskRepository(TaskMateDbContext dbContext) : ITaskRepository
{
    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> FindPageAsync(
        Guid userId,
        TaskQuery query,
        CancellationToken cancellationToken)
    {
        var tasks = dbContext.Tasks.AsNoTracking().Where(task => task.UserId == userId);
        if (query.Status is { } status)
        {
            tasks = tasks.Where(task => task.Status == status);
        }
        if (query.Category is { } category)
        {
            tasks = tasks.Where(task => task.Category == category);
        }
        if (query.Priority is { } priority)
        {
            tasks = tasks.Where(task => task.Priority == priority);
        }
        if (query.DueFrom is { } dueFrom)
        {
            tasks = tasks.Where(task => task.DueDate >= dueFrom);
        }
        if (query.DueThrough is { } dueThrough)
        {
            tasks = tasks.Where(task => task.DueDate <= dueThrough);
        }
        if (query.ExcludeCompleted)
        {
            tasks = tasks.Where(task => task.Status != TaskState.Completed);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            tasks = tasks.Where(task => EF.Functions.Like(task.Title, $"%{search}%", "\\"));
        }

        var totalCount = await tasks.CountAsync(cancellationToken);
        var pageItems = await tasks
            .OrderBy(task => task.DueDate == null)
            .ThenBy(task => task.DueDate)
            .ThenByDescending(task => task.Priority == TaskPriority.High ? 3 : task.Priority == TaskPriority.Medium ? 2 : 1)
            .ThenByDescending(task => task.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return (pageItems, totalCount);
    }

    public Task<TaskItem?> FindByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) =>
        dbContext.Tasks.SingleOrDefaultAsync(task => task.Id == taskId && task.UserId == userId, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        var userTasks = dbContext.Tasks.AsNoTracking().Where(task => task.UserId == userId);
        var total = await userTasks.CountAsync(cancellationToken);
        var completed = await userTasks.CountAsync(task => task.Status == TaskState.Completed, cancellationToken);
        var upcoming = await userTasks.CountAsync(task =>
            task.Status != TaskState.Completed && task.DueDate >= today && task.DueDate <= today.AddDays(7), cancellationToken);
        var overdue = await userTasks.CountAsync(task =>
            task.Status != TaskState.Completed && task.DueDate < today, cancellationToken);
        var percent = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1);
        return new DashboardResponse(today, total, completed, upcoming, overdue, percent);
    }
}
