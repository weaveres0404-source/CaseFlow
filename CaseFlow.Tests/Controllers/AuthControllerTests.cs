using CaseFlow.Server.Controllers;
using CaseFlow.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CaseFlow.Tests.Controllers;

/// <summary>
/// AuthController 單元測試。
///
/// 驗證範圍：
///  - Login：必填欄位驗證、帳號不存在、密碼錯誤、首次登入(MustChangePassword)、正常登入
///  - Me：能以 claim 正確取得當前使用者資訊
///  - SetupPassword：缺少參數驗證、密碼過短驗證、無效 token
/// </summary>
public class AuthControllerTests : ControllerTestBase
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthController _sut;

    public AuthControllerTests() : base(nameof(AuthControllerTests))
    {
        _configMock = new Mock<IConfiguration>();
        // 返回 null 讓控制器使用內建 fallback 金鑰（避免環境依賴）
        _configMock.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);

        _sut = new AuthController(_configMock.Object, Db);
    }

    // ─── Login ───────────────────────────────────────────────────────────────────

    /// <summary>Username 為空時應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Login_MissingUsername_ReturnsBadRequest()
    {
        // Arrange
        var req = new LoginRequest { Username = "", Password = "Password123" };

        // Act
        var result = await _sut.Login(req);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    /// <summary>Password 為空時應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var req = new LoginRequest { Username = "admin", Password = "" };

        // Act
        var result = await _sut.Login(req);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>帳號不存在時，回傳 200 但 success=false, code=UNAUTHORIZED。</summary>
    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorizedPayload()
    {
        // Arrange — 不植入任何使用者
        var req = new LoginRequest { Username = "ghost", Password = "Pass123" };

        // Act
        var result = await _sut.Login(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic? body = ok.Value;
        Assert.NotNull(body);
        // 反射讀取 anonymous type 屬性
        var success = (bool)body!.GetType().GetProperty("success")!.GetValue(body)!;
        Assert.False(success);
    }

    /// <summary>帳號存在但密碼錯誤時，回傳 200 success=false。</summary>
    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedPayload()
    {
        // Arrange
        await SeedUserAsync(userId: 1, username: "tester", email: "t@t.test");
        var req = new LoginRequest { Username = "tester", Password = "WrongPass" };

        // Act
        var result = await _sut.Login(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        var success = (bool)body.GetType().GetProperty("success")!.GetValue(body)!;
        Assert.False(success);
    }

    /// <summary>首次登入（MustChangePassword=true）時，回傳 setup_token 而非 access_token。</summary>
    [Fact]
    public async Task Login_MustChangePassword_ReturnsSetupToken()
    {
        // Arrange
        await SeedUserAsync(userId: 10, username: "newbie", email: "newbie@t.test", mustChange: true);
        var req = new LoginRequest { Username = "newbie", Password = "Password123" };

        // Act
        var result = await _sut.Login(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        bool success = (bool)body.GetType().GetProperty("success")!.GetValue(body)!;
        Assert.True(success);

        // data.must_change_password == true
        dynamic data = body.GetType().GetProperty("data")!.GetValue(body)!;
        bool mustChange = (bool)data.GetType().GetProperty("must_change_password")!.GetValue(data)!;
        Assert.True(mustChange);

        // setup_token 存在且非空
        string? setupToken = (string?)data.GetType().GetProperty("setup_token")!.GetValue(data);
        Assert.False(string.IsNullOrEmpty(setupToken));
    }

    /// <summary>正常登入成功時，回傳 200 success=true 且含 access_token 與使用者資訊。</summary>
    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        await SeedUserAsync(userId: 2, username: "validuser", email: "v@test.test", mustChange: false);
        var req = new LoginRequest { Username = "validuser", Password = "Password123" };

        // Act
        var result = await _sut.Login(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        bool success = (bool)body.GetType().GetProperty("success")!.GetValue(body)!;
        Assert.True(success);

        dynamic data = body.GetType().GetProperty("data")!.GetValue(body)!;
        string? token = (string?)data.GetType().GetProperty("access_token")!.GetValue(data);
        Assert.False(string.IsNullOrEmpty(token));

        bool mustChange = (bool)data.GetType().GetProperty("must_change_password")!.GetValue(data)!;
        Assert.False(mustChange);
    }

    // ─── Me ──────────────────────────────────────────────────────────────────────

    /// <summary>Me endpoint 應依 ClaimsPrincipal 中的 user_id 回傳對應使用者。</summary>
    [Fact]
    public async Task Me_AuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange
        await SeedUserAsync(userId: 5, username: "meuser", email: "me@test.test");
        _sut.ControllerContext = MakeControllerContext(userId: 5, role: "ADMIN");

        // Act
        var result = await _sut.Me();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic body = ok.Value!;
        bool success = (bool)body.GetType().GetProperty("success")!.GetValue(body)!;
        Assert.True(success);

        dynamic data = body.GetType().GetProperty("data")!.GetValue(body)!;
        string? username = (string?)data.GetType().GetProperty("username")!.GetValue(data);
        Assert.Equal("meuser", username);
    }

    /// <summary>Me endpoint 當 user_id 不存在時應回傳 404。</summary>
    [Fact]
    public async Task Me_UserNotFound_ReturnsNotFound()
    {
        // Arrange — 無任何使用者
        _sut.ControllerContext = MakeControllerContext(userId: 999, role: "ADMIN");

        // Act
        var result = await _sut.Me();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ─── SetupPassword ────────────────────────────────────────────────────────────

    /// <summary>缺少 setup_token 時應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task SetupPassword_MissingToken_ReturnsBadRequest()
    {
        // Arrange
        var req = new SetupPasswordRequest { SetupToken = "", NewPassword = "NewPass123" };

        // Act
        var result = await _sut.SetupPassword(req);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>密碼少於 8 字元時應回傳 400 BadRequest。</summary>
    [Fact]
    public async Task SetupPassword_ShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var req = new SetupPasswordRequest { SetupToken = "sometoken", NewPassword = "short" };

        // Act
        var result = await _sut.SetupPassword(req);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>無效的 JWT setup token 應回傳 401 Unauthorized。</summary>
    [Fact]
    public async Task SetupPassword_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var req = new SetupPasswordRequest { SetupToken = "totally.invalid.token", NewPassword = "ValidPass123" };

        // Act
        var result = await _sut.SetupPassword(req);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
