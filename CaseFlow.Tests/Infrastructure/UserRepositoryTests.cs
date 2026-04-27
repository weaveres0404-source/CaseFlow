using CaseFlow.Server.Data.Repositories;
using CaseFlow.Server.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CaseFlow.Tests.Infrastructure;

/// <summary>
/// UserRepository 單元測試，重點驗證 DB 唯一約束例外傳播行為。
///
/// 背景（init_db.sql 約束條件）：
///   PostgreSQL 的 users 資料表在生產環境中應有下列唯一約束：
///     UNIQUE (username)  — 對映：users_username_key
///     UNIQUE (email)     — 對映：users_email_key
///   EF Core InMemory 不執行 DB 層級唯一約束，因此使用 Moq
///   讓 SaveChangesAsync 模擬 Npgsql 回傳 error 23505 (unique_violation)
///   所產生的 DbUpdateException，確認 Repository 不會吞掉此例外。
/// </summary>
public class UserRepositoryTests
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private static DbContextOptions<CaseFlowDbContext> BuildOptions(string dbName) =>
        new DbContextOptionsBuilder<CaseFlowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    private static User BuildTestUser(string username, string email) =>
        new()
        {
            Username = username,
            PasswordHash = "hashed_pw",
            FullName = $"Test User ({username})",
            Email = email,
            Role = "SE",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    // ─────────────────────────────────────────────────────────────────────────────
    // AddAsync — 唯一約束違反（Moq 路徑）
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 當 username 已存在於 DB，Npgsql 會拋出包含 error 23505 的 DbUpdateException。
    /// Repository.AddAsync 應將例外原樣向上傳播，讓呼叫層決定如何回應。
    /// </summary>
    [Fact]
    public async Task AddAsync_DuplicateUsername_ThrowsDbUpdateException()
    {
        // Arrange
        var options = BuildOptions(nameof(AddAsync_DuplicateUsername_ThrowsDbUpdateException));

        var pgInner = new Exception(
            "23505: duplicate key value violates unique constraint \"users_username_key\"");
        var mockCtx = new Mock<CaseFlowDbContext>(options) { CallBase = true };
        mockCtx
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Unique constraint violation.", pgInner));

        var repo = new UserRepository(mockCtx.Object);
        var duplicateUser = BuildTestUser("existing_user", "new_email@mail.test");

        // Act
        Func<Task> act = () => repo.AddAsync(duplicateUser);

        // Assert
        var ex = await Assert.ThrowsAsync<DbUpdateException>(act);
        Assert.Contains("23505", ex.InnerException!.Message);
    }

    /// <summary>
    /// 當 email 已存在於 DB，Npgsql 會拋出包含 error 23505 的 DbUpdateException。
    /// Repository.AddAsync 應將例外原樣向上傳播。
    /// </summary>
    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsDbUpdateException()
    {
        // Arrange
        var options = BuildOptions(nameof(AddAsync_DuplicateEmail_ThrowsDbUpdateException));

        var pgInner = new Exception(
            "23505: duplicate key value violates unique constraint \"users_email_key\"");
        var mockCtx = new Mock<CaseFlowDbContext>(options) { CallBase = true };
        mockCtx
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Unique constraint violation.", pgInner));

        var repo = new UserRepository(mockCtx.Object);
        var duplicateEmailUser = BuildTestUser("new_username", "already_used@mail.test");

        // Act
        Func<Task> act = () => repo.AddAsync(duplicateEmailUser);

        // Assert
        var ex = await Assert.ThrowsAsync<DbUpdateException>(act);
        Assert.Contains("23505", ex.InnerException!.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // AddAsync — 正常路徑（InMemory 路徑）
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 新增唯一合法使用者時，回傳的實體 UserId 應由 EF 填入（> 0），
    /// 且欄位值與輸入相符。
    /// </summary>
    [Fact]
    public async Task AddAsync_UniqueUser_ReturnsSavedEntityWithGeneratedId()
    {
        // Arrange
        var options = BuildOptions(nameof(AddAsync_UniqueUser_ReturnsSavedEntityWithGeneratedId));
        await using var context = new CaseFlowDbContext(options);
        var repo = new UserRepository(context);
        var user = BuildTestUser("unique_user", "unique@mail.test");

        // Act
        var saved = await repo.AddAsync(user);

        // Assert
        Assert.True(saved.UserId > 0);
        Assert.Equal("unique_user", saved.Username);
        Assert.Equal("unique@mail.test", saved.Email);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>以存在的 UserId 查詢時，應回傳對應的 User 實體。</summary>
    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsMatchingUser()
    {
        // Arrange
        var options = BuildOptions(nameof(GetByIdAsync_ExistingId_ReturnsMatchingUser));
        await using var context = new CaseFlowDbContext(options);

        var user = BuildTestUser("findable_user", "find@mail.test");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByIdAsync(user.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("findable_user", result!.Username);
        Assert.Equal("find@mail.test", result.Email);
    }

    /// <summary>以不存在的 UserId 查詢時，應回傳 null 而非拋出例外。</summary>
    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var options = BuildOptions(nameof(GetByIdAsync_NonExistentId_ReturnsNull));
        await using var context = new CaseFlowDbContext(options);
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Moq 的 Setup 驗證：確認 SaveChangesAsync 確實在 AddAsync 中被呼叫了一次。
    /// </summary>
    [Fact]
    public async Task AddAsync_AlwaysCallsSaveChangesAsync_ExactlyOnce()
    {
        // Arrange
        var options = BuildOptions(nameof(AddAsync_AlwaysCallsSaveChangesAsync_ExactlyOnce));

        var mockCtx = new Mock<CaseFlowDbContext>(options) { CallBase = true };
        mockCtx
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // 模擬 1 row affected

        var repo = new UserRepository(mockCtx.Object);
        var user = BuildTestUser("verify_save_user", "verify@mail.test");

        // Act
        await repo.AddAsync(user);

        // Assert — 確認 SaveChangesAsync 被呼叫恰好一次
        mockCtx.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
