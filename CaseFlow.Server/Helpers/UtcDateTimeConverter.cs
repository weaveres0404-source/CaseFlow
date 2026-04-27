using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaseFlow.Server.Helpers;

/// <summary>
/// 將 <see cref="DateTime"/> 序列化為帶有 +08:00 時區偏移的 ISO-8601 字串，
/// 讓前端 <c>new Date(value)</c> 在任何瀏覽器時區都能正確解析為 UTC+8 時間。
///
/// 背景：資料庫欄位為 <c>timestamp without time zone</c>，儲存的是 UTC+8 時間，
/// 但 JSON 序列化若不附加時區資訊，前端在非 UTC+8 環境下會解析錯誤。
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string Fmt = "yyyy-MM-ddTHH:mm:ss.ffffff+08:00";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Fmt));
}

/// <summary>Nullable DateTime 版本。</summary>
public sealed class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly UtcDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return Inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else Inner.Write(writer, value.Value, options);
    }
}
