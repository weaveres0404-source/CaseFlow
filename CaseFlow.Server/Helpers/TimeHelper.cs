namespace CaseFlow.Server.Helpers;

/// <summary>
/// 提供系統統一使用的「當下時間」。
///
/// 資料庫欄位為 <c>timestamp without time zone</c>，儲存 UTC 時間值（Kind = Unspecified），
/// 前端透過 <c>UtcDateTimeConverter</c> 附加 Z 後綴後，以 Asia/Taipei 顯示為 UTC+8。
///
/// JWT token 到期時間等需符合 RFC 7519 標準（UTC）的場合，
/// 請繼續使用 <c>DateTime.UtcNow</c>。
/// </summary>
public static class TimeHelper
{
    /// <summary>UTC 的目前時間（Kind = Unspecified，適合寫入 timestamp without time zone 欄位）。</summary>
    public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}
