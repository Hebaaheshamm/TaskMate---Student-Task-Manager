using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskMate.Api.Data;

public sealed class TaskMateDbContextFactory : IDesignTimeDbContextFactory<TaskMateDbContext>
{
    public TaskMateDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaskMateDbContext>()
            .UseSqlite("Data Source=taskmate-design-time.db")
            .Options;
        return new TaskMateDbContext(options);
    }
}
