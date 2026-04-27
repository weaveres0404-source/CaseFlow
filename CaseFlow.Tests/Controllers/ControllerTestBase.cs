using CaseFlow.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CaseFlow.Tests.Controllers;

/// <summary>
/// 所有 Controller 測試的共用基底類別。
/// 提供：
///  - 獨立 InMemory DbContext 工廠
///  - 已驗證身份的 ControllerContext 建立器
///  - 測試用常用種子資料
/// </summary>
public abstract class ControllerTestBase : IDisposable
{
    protected readonly CaseFlowDbContext Db;

    protected ControllerTestBase(string dbName)
    {
        // 每個測試實例使用獨立的 InMemory 資料庫，避免測試間資料污染
        var uniqueName = $"{dbName}-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<CaseFlowDbContext>()
            .UseInMemoryDatabase(uniqueName)
            .Options;
        Db = new CaseFlowDbContext(options);
    }

    // ─── 身份驗證輔助 ───────────────────────────────────────────────────────────

    /// <summary>建立含有 user_id 與 Role claim 的 ControllerContext，模擬已驗證使用者。</summary>
    protected static ControllerContext MakeControllerContext(int userId, string role = "ADMIN")
    {
        var claims = new List<Claim>
        {
            new("user_id", userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ─── 種子資料輔助 ──────────────────────────────────────────────────────────

    protected async Task<User> SeedUserAsync(
        int userId = 1,
        string username = "admin",
        string email = "admin@test.test",
        string role = "ADMIN",
        bool mustChange = false)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            UserId = userId,
            Username = username,
            PasswordHash = "Password123",
            FullName = $"Test {username}",
            Email = email,
            Role = role,
            IsActive = true,
            MustChangePassword = mustChange,
            CreatedAt = now,
            UpdatedAt = now
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    protected async Task<Customer> SeedCustomerAsync(
        int customerId = 1,
        string name = "Test Customer",
        bool isActive = true)
    {
        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            CustomerId = customerId,
            CustomerName = name,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();
        return customer;
    }

    protected async Task<Project> SeedProjectAsync(
        int projectId = 1,
        int customerId = 1,
        string code = "PRJ-001",
        string name = "Test Project",
        bool isActive = true)
    {
        var now = DateTime.UtcNow;
        var project = new Project
        {
            ProjectId = projectId,
            ProjectCode = code,
            ProjectName = name,
            CustomerId = customerId,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        Db.Projects.Add(project);
        await Db.SaveChangesAsync();
        return project;
    }

    protected async Task<ProblemCategory> SeedCategoryAsync(
        int categoryId = 1,
        string name = "Test Category",
        int sortOrder = 1,
        bool isActive = true)
    {
        var now = DateTime.UtcNow;
        var cat = new ProblemCategory
        {
            CategoryId = categoryId,
            CategoryName = name,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
        Db.ProblemCategories.Add(cat);
        await Db.SaveChangesAsync();
        return cat;
    }

    // ─── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose() => Db.Dispose();
}
