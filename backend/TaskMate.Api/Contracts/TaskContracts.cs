using System.ComponentModel.DataAnnotations;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Contracts;

public sealed record CreateTaskRequest(
    [param: Required, StringLength(120, MinimumLength = 1)] string Title,
    [param: StringLength(5000)] string? Description = null,
    DateOnly? DueDate = null,
    [param: EnumDataType(typeof(TaskPriority))] TaskPriority Priority = TaskPriority.Medium,
    [param: EnumDataType(typeof(TaskState))] TaskState Status = TaskState.ToDo,
    [param: EnumDataType(typeof(TaskCategory))] TaskCategory Category = TaskCategory.Assignment);

public sealed record UpdateTaskRequest(
    [param: Required, StringLength(120, MinimumLength = 1)] string Title,
    [param: StringLength(5000)] string? Description,
    DateOnly? DueDate,
    [param: EnumDataType(typeof(TaskPriority))] TaskPriority Priority,
    [param: EnumDataType(typeof(TaskState))] TaskState Status,
    [param: EnumDataType(typeof(TaskCategory))] TaskCategory Category);

public sealed record TaskQuery(
    TaskState? Status,
    TaskCategory? Category,
    TaskPriority? Priority,
    string? Search,
    DateOnly? DueFrom = null,
    DateOnly? DueThrough = null,
    bool ExcludeCompleted = false,
    int Page = 1,
    int PageSize = 20);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    DateOnly? DueDate,
    TaskPriority Priority,
    TaskState Status,
    TaskCategory Category,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record DashboardResponse(
    DateOnly Today,
    int TotalTasks,
    int CompletedTasks,
    int UpcomingDeadlines,
    int OverdueTasks,
    double CompletionPercent);
