using CaseFlow.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly CaseFlowDbContext _db;

    public UserRepository(CaseFlowDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Users.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
