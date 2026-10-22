using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Repositories;

public sealed class UserRepository(TaskMateDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
