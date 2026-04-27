namespace CaseFlow.Server.Helpers;

/// <summary>
/// 提供系統統一使用的「當下時間」，固定為 UTC+8（台灣/中華民國標準時間）。
///
/// 所有需要寫入資料庫的時間戳記皆應使用 <see cref="Now"/>，
/// 確保資料庫欄位儲存的時間與業務時區一致。
///
/// JWT token 到期時間等需符合 RFC 7519 標準（UTC）的場合，
/// 請繼續使用 <c>DateTime.UtcNow</c>。
/// </summary>
public static class TimeHelper
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(8);

    /// <summary>UTC+8 的目前時間（Kind = Unspecified，對應資料庫 timestamp without time zone）。</summary>
    public static DateTime Now => DateTime.UtcNow.Add(Offset);

    /// <summary>將指定 UTC 時間轉換為 UTC+8。</summary>
    public static DateTime ToLocal(DateTime utc) => utc.Add(Offset);
}
