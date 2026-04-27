using CaseFlow.Server.Models;

namespace CaseFlow.Server.Data.Repositories;

public interface IUserRepository
{
    /// <summary>依 UserId 查詢使用者。</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增使用者；若違反 DB 唯一約束（username 或 email 重複）
    /// 會從 Npgsql 透過 EF Core 拋出 DbUpdateException（PostgreSQL error 23505）。
    /// </summary>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
}
