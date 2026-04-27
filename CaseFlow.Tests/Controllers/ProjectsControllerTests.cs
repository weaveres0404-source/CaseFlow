using CaseFlow.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CaseFlow.Tests.Controllers;

/// <summary>
/// ProjectsController 單元測試。
///
/// 驗證範圍：
///  - GetAll：空列表、僅回傳 active、分頁 meta
///  - GetById：找到/找不到
///  - Create：必填驗證、成功建立
///  - Update (PATCH)：找不到 → 404、更新欄位
///  - Delete：找不到 → 404、軟刪除
/// </summary>
public class ProjectsControllerTests : ControllerTestBase
{
    private readonly ProjectsController _sut;

    public ProjectsControllerTests() : base(nameof(ProjectsControllerTests))
    {
        _sut = new ProjectsController(Db)
        {
            ControllerContext = MakeControllerContext(userId: 1)
        };
    }

    // ─── GetAll ───────────────────────────────────────────────────────────────────

    /// <summary>空資料庫時回傳空列表且 total=0。</summary>
    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        int total = (int)meta.GetType().GetProperty("total")!.GetValue(meta)!;
        Assert.Equal(0, total);
    }

    /// <summary>GetAll 只回傳 is_active=true 的專案。</summary>
    [Fact]
    public async Task GetAll_ExcludesInactiveProjects()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 1);
        await SeedProjectAsync(projectId: 1, customerId: 1, code: "A", name: "Active Proj", isActive: true);
        await SeedProjectAsync(projectId: 2, customerId: 1, code: "B", name: "Inactive Proj", isActive: false);

        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        int total = (int)meta.GetType().GetProperty("total")!.GetValue(meta)!;
        Assert.Equal(1, total);
    }

    /// <summary>回傳的 meta 應包含正確的 page 與 page_size。</summary>
    [Fact]
    public async Task GetAll_MetaContainsCorrectPageInfo()
    {
        // Arrange — 無資料

        // Act
        var result = await _sut.GetAll(page: 2, page_size: 5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        Assert.Equal(2, (int)meta.GetType().GetProperty("page")!.GetValue(meta)!);
        Assert.Equal(5, (int)meta.GetType().GetProperty("page_size")!.GetValue(meta)!);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────────

    /// <summary>存在的 ID 應回傳 200 與完整專案資料。</summary>
    [Fact]
    public async Task GetById_ExistingId_ReturnsProjectWithCustomer()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 1, name: "Acme Corp");
        await SeedProjectAsync(projectId: 10, customerId: 1, code: "ACME-01", name: "Portal Project");

        // Act
        var result = await _sut.GetById(10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic data = body.GetType().GetProperty("data")!.GetValue(body)!;
        Assert.Equal("Portal Project", (string)data.GetType().GetProperty("project_name")!.GetValue(data)!);
        Assert.Equal("ACME-01", (string)data.GetType().GetProperty("project_code")!.GetValue(data)!);
    }

    /// <summary>不存在的 ID 應回傳 404 NotFound。</summary>
    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetById(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── Create ───────────────────────────────────────────────────────────────────

    /// <summary>缺少 project_code 應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Create_MissingProjectCode_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ProjectCreateDto { ProjectCode = "", ProjectName = "Valid Name", CustomerId = 1 };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>缺少 project_name 應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Create_MissingProjectName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ProjectCreateDto { ProjectCode = "CODE-001", ProjectName = "", CustomerId = 1 };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>合法輸入應建立專案並回傳 201 CreatedAtAction。</summary>
    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 1);
        var dto = new ProjectCreateDto
        {
            ProjectCode = "NEW-001",
            ProjectName = "Brand New Project",
            CustomerId = 1,
            Description = "Test project"
        };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(1, Db.Projects.Count());
        Assert.Equal("NEW-001", Db.Projects.Single().ProjectCode);
    }

    /// <summary>建立時代碼前後空白應被 Trim。</summary>
    [Fact]
    public async Task Create_CodeAndNameWithWhitespace_TrimsValues()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 1);
        var dto = new ProjectCreateDto
        {
            ProjectCode = "  TRIM-001  ",
            ProjectName = "  Trim Project  ",
            CustomerId = 1
        };

        // Act
        await _sut.Create(dto);

        // Assert
        var saved = Db.Projects.Single();
        Assert.Equal("TRIM-001", saved.ProjectCode);
        Assert.Equal("Trim Project", saved.ProjectName);
    }

    // ─── Update (PATCH) ───────────────────────────────────────────────────────────

    /// <summary>更新不存在的專案應回傳 404 NotFound。</summary>
    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var dto = new ProjectCreateDto { ProjectName = "Any" };

        // Act
        var result = await _sut.Update(99999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>更新存在的專案時，名稱應正確寫入。</summary>
    [Fact]
    public async Task Update_ExistingId_UpdatesProjectName()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 1);
        await SeedProjectAsync(projectId: 5, customerId: 1, code: "OLD-001", name: "Old Name");
        var dto = new ProjectCreateDto { ProjectName = "New Name" };

        // Act
        var result = await _sut.Update(5, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var updated = Db.Projects.Single(p => p.ProjectId == 5);
        Assert.Equal("New Name", updated.ProjectName);
    }

    // ─── Delete (soft-delete) ─────────────────────────────────────────────────────

    /// <summary>刪除不存在的專案應回傳 404 NotFound。</summary>
    [Fact]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _sut.Delete(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>軟刪除後 is_active 應設為 false，記錄仍保留。</summary>
    [Fact]
    public async Task Delete_ExistingId_SetsIsActiveFalse()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 1);
        await SeedProjectAsync(projectId: 8, customerId: 1, code: "DEL-001", name: "Doomed Project");

        // Act
        var result = await _sut.Delete(8);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var entity = Db.Projects.Single(p => p.ProjectId == 8);
        Assert.False(entity.IsActive);
        Assert.Equal(1, Db.Projects.Count()); // 記錄仍存在
    }
}
