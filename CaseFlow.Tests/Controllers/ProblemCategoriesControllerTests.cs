using CaseFlow.Server.Controllers;
using CaseFlow.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CaseFlow.Tests.Controllers;

/// <summary>
/// ProblemCategoriesController 單元測試。
///
/// 驗證範圍：
///  - GetList：空列表、active-only 過濾、分頁 meta
///  - GetById：找到/找不到、只回傳 active 分類
///  - Create：缺少名稱驗證、成功建立
///  - Update (PUT)：找不到、更新欄位
///  - Delete：找不到、已停用再刪除應 400、正常軟刪除
/// </summary>
public class ProblemCategoriesControllerTests : ControllerTestBase
{
    private readonly ProblemCategoriesController _sut;

    public ProblemCategoriesControllerTests() : base(nameof(ProblemCategoriesControllerTests))
    {
        _sut = new ProblemCategoriesController(Db)
        {
            ControllerContext = MakeControllerContext(userId: 1)
        };
    }

    // ─── GetList ──────────────────────────────────────────────────────────────────

    /// <summary>空資料庫回傳空列表且 total=0。</summary>
    [Fact]
    public async Task GetList_EmptyDb_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetList();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        var items = body.GetType().GetProperty("data")!.GetValue(body) as System.Collections.IEnumerable;
        Assert.NotNull(items);
        Assert.Empty(items!.Cast<object>());

        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        int total = (int)meta.GetType().GetProperty("total")!.GetValue(meta)!;
        Assert.Equal(0, total);
    }

    /// <summary>GetList 只應回傳 is_active=true 的分類（inactive 不出現）。</summary>
    [Fact]
    public async Task GetList_ExcludesInactiveCategories()
    {
        // Arrange
        await SeedCategoryAsync(categoryId: 1, name: "Active Cat", isActive: true);
        await SeedCategoryAsync(categoryId: 2, name: "Inactive Cat", isActive: false);

        // Act
        var result = await _sut.GetList();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        int total = (int)meta.GetType().GetProperty("total")!.GetValue(meta)!;
        Assert.Equal(1, total);
    }

    /// <summary>page_size 超過 100 應正規化為 20。</summary>
    [Fact]
    public async Task GetList_PageSizeExceedsMax_NormalizesTo20()
    {
        // Act
        var result = await _sut.GetList(page: 1, page_size: 200);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic meta = body.GetType().GetProperty("meta")!.GetValue(body)!;
        int pageSize = (int)meta.GetType().GetProperty("page_size")!.GetValue(meta)!;
        Assert.Equal(20, pageSize);
    }

    /// <summary>多筆資料時應依 sort_order 升序回傳。</summary>
    [Fact]
    public async Task GetList_ReturnsItemsOrderedBySortOrder()
    {
        // Arrange
        await SeedCategoryAsync(categoryId: 1, name: "Third", sortOrder: 3);
        await SeedCategoryAsync(categoryId: 2, name: "First", sortOrder: 1);
        await SeedCategoryAsync(categoryId: 3, name: "Second", sortOrder: 2);

        // Act
        var result = await _sut.GetList();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        var items = ((System.Collections.IEnumerable)body.GetType().GetProperty("data")!.GetValue(body)!)
                    .Cast<dynamic>()
                    .ToList();

        Assert.Equal("First", (string)items[0].GetType().GetProperty("name")!.GetValue(items[0])!);
        Assert.Equal("Second", (string)items[1].GetType().GetProperty("name")!.GetValue(items[1])!);
        Assert.Equal("Third", (string)items[2].GetType().GetProperty("name")!.GetValue(items[2])!);
    }

    // ─── GetById ─────────────────────────────────────────────────────────────────

    /// <summary>存在且 active 的分類應正確回傳。</summary>
    [Fact]
    public async Task GetById_ExistingActiveCategory_ReturnsOk()
    {
        // Arrange
        await SeedCategoryAsync(categoryId: 5, name: "Network Issue");

        // Act
        var result = await _sut.GetById(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        dynamic data = body.GetType().GetProperty("data")!.GetValue(body)!;
        string? name = (string?)data.GetType().GetProperty("name")!.GetValue(data);
        Assert.Equal("Network Issue", name);
    }

    /// <summary>不存在的 ID 應回傳 404。</summary>
    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetById(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>inactive 分類的 ID 應回傳 404（GetById 僅查 active）。</summary>
    [Fact]
    public async Task GetById_InactiveCategory_ReturnsNotFound()
    {
        // Arrange
        await SeedCategoryAsync(categoryId: 7, name: "Deleted Cat", isActive: false);

        // Act
        var result = await _sut.GetById(7);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── Create ───────────────────────────────────────────────────────────────────

    /// <summary>null dto 應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Create_NullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Create(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>空白 CategoryName 應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Create_BlankCategoryName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new ProblemCategory { CategoryName = "  " };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>合法 dto 應建立分類並回傳 201 CreatedAtAction。</summary>
    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new ProblemCategory
        {
            CategoryName = "Hardware Failure",
            Description = "Any hardware related",
            SortOrder = 1,
            IsActive = true
        };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(1, Db.ProblemCategories.Count());
        Assert.Equal("Hardware Failure", Db.ProblemCategories.Single().CategoryName);
    }

    // ─── Update (PUT) ─────────────────────────────────────────────────────────────

    /// <summary>更新不存在的分類應回傳 404 NotFound。</summary>
    [Fact]
    public async Task Update_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var dto = new ProblemCategory { CategoryName = "Anything", SortOrder = 1, IsActive = true };

        // Act
        var result = await _sut.Update(99999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>更新存在的分類時，欄位應正確寫入。</summary>
    [Fact]
    public async Task Update_ExistingId_UpdatesFields()
    {
        // Arrange
        await SeedCategoryAsync(categoryId: 10, name: "OldName", sortOrder: 1);
        var dto = new ProblemCategory
        {
            CategoryName = "UpdatedName",
            Description = "New desc",
            SortOrder = 5,
            IsActive = true
        };

        // Act
        var result = await _sut.Update(10, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var updated = Db.ProblemCategories.Single(c => c.CategoryId == 10);
        Assert.Equal("UpdatedName", updated.CategoryName);
        Assert.Equal(5, updated.SortOrder);
        Assert.Equal("New desc", updated.Description);
    }

    // ─── Delete (soft-delete) ─────────────────────────────────────────────────────

    /// <summary>刪除不存在的分類應回傳 404 NotFound。</summary>
    [Fact]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _sut.Delete(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>對已停用的分類再次刪除應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Delete_AlreadyInactive_ReturnsBadRequest()
    {
        // Arrange
        await SeedCategoryAsync(categoryId: 15, name: "Already Gone", isActive: false);

        // Act
        var result = await _sut.Delete(15);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>正常軟刪除後，is_active 應設為 false，記錄仍存在。</summary>
    [Fact]
    public async Task Delete_ActiveCategory_SetsIsActiveFalse()
    {
        // Arrange
        await SeedCategoryAsync(categoryId: 20, name: "To Delete", isActive: true);

        // Act
        var result = await _sut.Delete(20);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var entity = Db.ProblemCategories.Single(c => c.CategoryId == 20);
        Assert.False(entity.IsActive);
        Assert.Equal(1, Db.ProblemCategories.Count());
    }
}
