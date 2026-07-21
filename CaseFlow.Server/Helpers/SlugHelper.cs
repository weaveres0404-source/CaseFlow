using Sqids;

namespace CaseFlow.Server.Helpers;

/// <summary>
/// 將整數案件 ID 轉換為不可猜測的短碼（URL slug），
/// 避免前端 URL 直接暴露流水號。
/// </summary>
public static class SlugHelper
{
    // 預設亂序字母表（26英大 + 26英小 + 10數字，固定排列）
    // 若 appsettings 有設定 Slugs:Alphabet 則會被覆蓋。
    private const string DefaultAlphabet =
        "k3mB9vXnR5pLzYqW7dJeAhsT2cFUg6iNuOoI4wKtGyZf8DaMbCrQlHjxEP1VS0";

    private static readonly SqidsEncoder<int> _encoder = CreateEncoder(null);

    /// <summary>初始化（由 Program.cs 在啟動時呼叫，可傳入 config 覆寫字母表）。</summary>
    public static void Configure(string? alphabetOverride)
    {
        var alphabet = string.IsNullOrWhiteSpace(alphabetOverride) ? DefaultAlphabet : alphabetOverride;

        // 重新建立 encoder（反射覆寫唯讀欄位，因 static 只允許初始化一次）
        var enc = CreateEncoder(alphabet);
        typeof(SlugHelper)
            .GetField("_encoder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .SetValue(null, enc);
    }

    private static SqidsEncoder<int> CreateEncoder(string? alphabet)
        => new SqidsEncoder<int>(new SqidsOptions
        {
            Alphabet = string.IsNullOrWhiteSpace(alphabet) ? DefaultAlphabet : alphabet,
            MinLength = 8
        });

    /// <summary>整數 ID → URL slug（例如 32 → "k9mBXnR5"）。</summary>
    public static string Encode(int id) => _encoder.Encode(id);

    /// <summary>URL slug → 整數 ID；無法解碼或長度不符時回傳 null。</summary>
    public static int? Decode(string slug)
    {
        // 嚴格檢查：長度必須 >= MinLength(8)，避免 "16" 之類短字串被誤判為合法 slug
        if (string.IsNullOrWhiteSpace(slug) || slug.Length < 8) return null;
        IReadOnlyList<int> ids;
        try
        {
            ids = _encoder.Decode(slug);
        }
        catch
        {
            // Sqids 在 overflow 或非預期輸入時可能丟例外；視為無效 slug
            return null;
        }
        if (ids.Count != 1) return null;
        // 回填驗證：重新 Encode 後必須等同原 slug，杜絕同 ID 多解
        try
        {
            return _encoder.Encode(ids[0]) == slug ? ids[0] : null;
        }
        catch
        {
            return null;
        }
    }
}
