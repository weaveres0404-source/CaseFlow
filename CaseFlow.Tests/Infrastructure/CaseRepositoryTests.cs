using CaseFlow.Server.Data.Repositories;
using CaseFlow.Server.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CaseFlow.Tests.Infrastructure;

/// <summary>
/// CaseRepository 單元測試。
///
/// 測試策略：
///   - DbUpdateException 路徑：使用 Moq 攔截 SaveChangesAsync，
///     模擬 PostgreSQL 違反 cases_case_number_key 唯一約束（error 23505）
///     而拋出的例外，驗證 Repository 不會吞掉此例外。
///   - Include 路徑：使用 EF Core InMemory 資料庫，
///     確認 GetByIdAsync 正確透過 Include 載入 Project 與 CreatedByNavigation(User)。
/// </summary>
public class CaseRepositoryTests
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>每個測試使用獨立的 InMemory 資料庫，避免測試間資料汙染。</summary>
    private static DbContextOptions<CaseFlowDbContext> BuildOptions(string dbName) =>
        new DbContextOptionsBuilder<CaseFlowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    /// <summary>
    /// 在指定 context 中植入必要的外鍵實體（User / Customer / Project / ProblemCategory），
    /// 並以呼叫端給定的 PK 值儲存，確保 FK 參照一致。
    /// </summary>
    private static async Task SeedLookupDataAsync(
        CaseFlowDbContext context,
        int userId = 1,
        int customerId = 1,
        int projectId = 1,
        int categoryId = 1)
    {
        var now = DateTime.UtcNow;

        context.Users.Add(new User
        {
            UserId = userId,
            Username = "seed_user",
            PasswordHash = "hashed",
            FullName = "Seed User",
            Email = "seed@caseflow.test",
            Role = "SE",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        context.Customers.Add(new Customer
        {
            CustomerId = customerId,
            CustomerName = "Seed Customer",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        context.Projects.Add(new Project
        {
            ProjectId = projectId,
            ProjectCode = "PRJ-T01",
            ProjectName = "Seed Project",
            CustomerId = customerId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        context.ProblemCategories.Add(new ProblemCategory
        {
            CategoryId = categoryId,
            CategoryName = "Seed Category",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.SaveChangesAsync();
    }

    /// <summary>建立最低限度合法的 Case 物件（不含 CaseId，由 EF 產生）。</summary>
    private static Case BuildMinimalCase(
        string caseNumber,
        int projectId = 1,
        int customerId = 1,
        int categoryId = 1,
        int createdBy = 1) =>
        new()
        {
            CaseNumber = caseNumber,
            ProjectId = projectId,
            CustomerId = customerId,
            CategoryId = categoryId,
            CreatedBy = createdBy,
            ReporterName = "Unit Test Reporter",
            CaseType = "REPAIR",
            Priority = "MEDIUM",
            Description = "Unit test stub case",
            Status = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    // ─────────────────────────────────────────────────────────────────────────────
    // AddAsync
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 當 SaveChangesAsync 因 DB 唯一約束（cases_case_number_key）
    /// 拋出 DbUpdateException 時，Repository 應原樣傳播，不應吞掉例外。
    ///
    /// 對應 init_db.sql 約束：
    ///   CONSTRAINT cases_case_number_key UNIQUE (case_number)
    ///   — 模型側對映：[Index("CaseNumber", Name = "cases_case_number_key", IsUnique = true)]
    /// </summary>
    [Fact]
    public async Task AddAsync_DuplicateCaseNumber_ThrowsDbUpdateException()
    {
        // Arrange
        var options = BuildOptions(nameof(AddAsync_DuplicateCaseNumber_ThrowsDbUpdateException));

        // 模擬 PostgreSQL 23505 (unique_violation) 的 inner exception
        var pgException = new Exception(
            "23505: duplicate key value violates unique constraint \"cases_case_number_key\"");

        // CallBase = true：除 SaveChangesAsync 外，其餘呼叫皆走真實 InMemory 實作
        var mockCtx = new Mock<CaseFlowDbContext>(options) { CallBase = true };
        mockCtx
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Unique constraint violation.", pgException));

        var repo = new CaseRepository(mockCtx.Object);
        var duplicate = BuildMinimalCase("CASE-DUPE-001");

        // Act
        Func<Task> act = () => repo.AddAsync(duplicate);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(act);
    }

    /// <summary>
    /// 新增合法（不重複）案件時，SaveChangesAsync 成功，
    /// 回傳的實體應已取得 DB 產生的 CaseId（> 0）。
    /// </summary>
    [Fact]
    public async Task AddAsync_ValidCase_ReturnsCaseWithGeneratedId()
    {
        // Arrange
        var options = BuildOptions(nameof(AddAsync_ValidCase_ReturnsCaseWithGeneratedId));
        await using var context = new CaseFlowDbContext(options);
        await SeedLookupDataAsync(context);

        var repo = new CaseRepository(context);
        var caseModel = BuildMinimalCase("CASE-NEW-001");

        // Act
        var saved = await repo.AddAsync(caseModel);

        // Assert
        Assert.True(saved.CaseId > 0);
        Assert.Equal("CASE-NEW-001", saved.CaseNumber);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GetByIdAsync 必須透過 Include 載入 Project 與 CreatedByNavigation(User)，
    /// 使呼叫端不需再發出額外查詢即可存取關聯實體。
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ExistingId_LoadsProjectAndCreatedByUser()
    {
        // Arrange ─ 使用獨立的 seedContext 寫入資料
        var options = BuildOptions(nameof(GetByIdAsync_ExistingId_LoadsProjectAndCreatedByUser));

        int savedCaseId;
        await using (var seedCtx = new CaseFlowDbContext(options))
        {
            await SeedLookupDataAsync(seedCtx, userId: 1, customerId: 1, projectId: 1, categoryId: 1);

            var caseEntity = BuildMinimalCase("CASE-INCLUDE-001");
            seedCtx.Cases.Add(caseEntity);
            await seedCtx.SaveChangesAsync();
            savedCaseId = caseEntity.CaseId;
        }

        // 使用新的 context 執行查詢，排除 EF 快取影響，確認 Include 真的有作用
        await using var readCtx = new CaseFlowDbContext(options);
        var repo = new CaseRepository(readCtx);

        // Act
        var result = await repo.GetByIdAsync(savedCaseId);

        // Assert ─ 實體本身
        Assert.NotNull(result);
        Assert.Equal("CASE-INCLUDE-001", result!.CaseNumber);

        // Assert ─ Project 已透過 Include 載入
        Assert.NotNull(result.Project);
        Assert.Equal("Seed Project", result.Project.ProjectName);
        Assert.Equal("PRJ-T01", result.Project.ProjectCode);

        // Assert ─ CreatedByNavigation (User) 已透過 Include 載入
        Assert.NotNull(result.CreatedByNavigation);
        Assert.Equal("seed_user", result.CreatedByNavigation.Username);
        Assert.Equal("Seed User", result.CreatedByNavigation.FullName);
    }

    /// <summary>查詢不存在的 CaseId 時，應回傳 null 而非拋出例外。</summary>
    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var options = BuildOptions(nameof(GetByIdAsync_NonExistentId_ReturnsNull));
        await using var context = new CaseFlowDbContext(options);
        var repo = new CaseRepository(context);

        // Act
        var result = await repo.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 確認不同的兩筆案件可以獨立查詢，Include 不會混淆 Project / User。
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_MultipleCases_EachLoadsOwnProject()
    {
        // Arrange
        var options = BuildOptions(nameof(GetByIdAsync_MultipleCases_EachLoadsOwnProject));
        int case1Id, case2Id;

        await using (var seedCtx = new CaseFlowDbContext(options))
        {
            var now = DateTime.UtcNow;

            seedCtx.Users.Add(new User
            {
                UserId = 1,
                Username = "user1",
                PasswordHash = "h",
                FullName = "User One",
                Email = "u1@t.test",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            seedCtx.Customers.Add(new Customer
            {
                CustomerId = 1,
                CustomerName = "Cust A",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            // 兩個不同的 Project
            seedCtx.Projects.Add(new Project
            {
                ProjectId = 1,
                ProjectCode = "A",
                ProjectName = "Project Alpha",
                CustomerId = 1,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            seedCtx.Projects.Add(new Project
            {
                ProjectId = 2,
                ProjectCode = "B",
                ProjectName = "Project Beta",
                CustomerId = 1,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            seedCtx.ProblemCategories.Add(new ProblemCategory
            {
                CategoryId = 1,
                CategoryName = "Cat",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            await seedCtx.SaveChangesAsync();

            var c1 = BuildMinimalCase("CASE-ALPHA", projectId: 1);
            var c2 = BuildMinimalCase("CASE-BETA", projectId: 2);
            seedCtx.Cases.AddRange(c1, c2);
            await seedCtx.SaveChangesAsync();
            case1Id = c1.CaseId;
            case2Id = c2.CaseId;
        }

        await using var readCtx = new CaseFlowDbContext(options);
        var repo = new CaseRepository(readCtx);

        // Act
        var result1 = await repo.GetByIdAsync(case1Id);
        var result2 = await repo.GetByIdAsync(case2Id);

        // Assert
        Assert.NotNull(result1!.Project);
        Assert.Equal("Project Alpha", result1.Project.ProjectName);

        Assert.NotNull(result2!.Project);
        Assert.Equal("Project Beta", result2.Project.ProjectName);
    }
}
