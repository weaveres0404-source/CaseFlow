using CaseFlow.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CaseFlow.Tests.Controllers;

/// <summary>
/// CustomersController 單元測試。
///
/// 驗證範圍：
///  - GetAll：空資料庫回傳空列表、分頁 meta 正確、僅回傳 is_active=true 的客戶
///  - GetById：找到/找不到
///  - Create：必填驗證、成功建立 → 201 CreatedAtAction
///  - Update (PATCH)：找不到 → 404、正確欄位更新
///  - Delete (soft-delete)：找不到 → 404、is_active 設為 false
/// </summary>
public class CustomersControllerTests : ControllerTestBase
{
    private readonly CustomersController _sut;

    public CustomersControllerTests() : base(nameof(CustomersControllerTests))
    {
        _sut = new CustomersController(Db)
        {
            ControllerContext = MakeControllerContext(userId: 1)
        };
    }

    // ─── GetAll ───────────────────────────────────────────────────────────────────

    /// <summary>空資料庫時回傳 200，data 為空陣列，total=0。</summary>
    [Fact]
    public async Task GetAll_EmptyDb_ReturnsEmptyList()
    {
        // Arrange — 無資料

        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        var items = body.GetType().GetProperty("data")!.GetValue(body) as System.Collections.IEnumerable;
        Assert.NotNull(items);
        Assert.Empty(items!.Cast<object>());
    }

    /// <summary>回傳 meta 的 total 等於資料庫中 active 客戶數量。</summary>
    [Fact]
    public async Task GetAll_WithData_ReturnsCorrectTotal()
    {
        // Arrange
        await SeedCustomerAsync(1, "Alpha Inc");
        await SeedCustomerAsync(2, "Beta Corp");
        await SeedCustomerAsync(3, "Inactive Co", isActive: false);

        // Act
        var result = await _sut.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        int total = (int)meta.GetType().GetProperty("total")!.GetValue(meta)!;
        Assert.Equal(2, total); // 只計 is_active=true
    }

    /// <summary>page_size 參數超過 100 時應自動修正為 20。</summary>
    [Fact]
    public async Task GetAll_PageSizeExceedsMax_NormalizesTo20()
    {
        // Arrange — 無資料

        // Act
        var result = await _sut.GetAll(page: 1, page_size: 999);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        int pageSize = (int)meta.GetType().GetProperty("page_size")!.GetValue(meta)!;
        Assert.Equal(20, pageSize);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────────

    /// <summary>存在的 ID 應回傳 200 與正確客戶名稱。</summary>
    [Fact]
    public async Task GetById_ExistingId_ReturnsCustomer()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 10, name: "Specific Corp");

        // Act
        var result = await _sut.GetById(10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic data = body.GetType().GetProperty("data")!.GetValue(body)!;
        string? name = (string?)data.GetType().GetProperty("customer_name")!.GetValue(data);
        Assert.Equal("Specific Corp", name);
    }

    /// <summary>不存在的 ID 應回傳 404 NotFound。</summary>
    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        // Arrange — 無資料

        // Act
        var result = await _sut.GetById(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── Create ───────────────────────────────────────────────────────────────────

    /// <summary>缺少 customer_name 時應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CustomerDto { CustomerName = "  " };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>合法輸入時應建立客戶並回傳 201 CreatedAtAction。</summary>
    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new CustomerDto { CustomerName = "New Client", ContactPhone = "0912345678" };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        // 資料庫確實寫入
        Assert.Equal(1, Db.Customers.Count());
    }

    /// <summary>建立時 CustomerName 前後空白應被 Trim。</summary>
    [Fact]
    public async Task Create_NameWithWhitespace_TrimsName()
    {
        // Arrange
        var dto = new CustomerDto { CustomerName = "  Trimmed Name  " };

        // Act
        await _sut.Create(dto);

        // Assert
        var saved = Db.Customers.Single();
        Assert.Equal("Trimmed Name", saved.CustomerName);
    }

    // ─── Update (PATCH) ───────────────────────────────────────────────────────────

    /// <summary>更新不存在的客戶應回傳 404 NotFound。</summary>
    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var dto = new CustomerDto { CustomerName = "Anything" };

        // Act
        var result = await _sut.Update(99999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>更新存在的客戶時，應正確更新名稱並回傳 200 OK。</summary>
    [Fact]
    public async Task Update_ExistingId_UpdatesNameAndReturnsOk()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 20, name: "OldName");
        var dto = new CustomerDto { CustomerName = "NewName" };

        // Act
        var result = await _sut.Update(20, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var updated = Db.Customers.Single(c => c.CustomerId == 20);
        Assert.Equal("NewName", updated.CustomerName);
    }

    // ─── Delete (soft-delete) ─────────────────────────────────────────────────────

    /// <summary>刪除不存在的客戶應回傳 404 NotFound。</summary>
    [Fact]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _sut.Delete(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>軟刪除後，is_active 應設為 false 而非真正移除記錄。</summary>
    [Fact]
    public async Task Delete_ExistingId_SetsIsActiveFalse()
    {
        // Arrange
        await SeedCustomerAsync(customerId: 30, name: "To Be Deleted");

        // Act
        var result = await _sut.Delete(30);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var entity = Db.Customers.Single(c => c.CustomerId == 30);
        Assert.False(entity.IsActive);
        // 記錄仍存在資料庫
        Assert.Equal(1, Db.Customers.Count());
    }
}
