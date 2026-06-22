using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaseFlow.Server.Helpers;

/// <summary>
/// Serializes timestamp-without-time-zone values as local wall-clock ISO strings.
/// Do not append "Z"; these values are already Asia/Taipei wall-clock time.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.ffffff";
    private static readonly TimeZoneInfo TaipeiTimeZone = ResolveTaipeiTimeZone();

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Unspecified);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(ToTaipeiWallClock(value).ToString(Format));

    private static DateTime ToTaipeiWallClock(DateTime value)
    {
        var wallClock = value.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(value, TaipeiTimeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(value, TaipeiTimeZone),
            _ => value
        };

        return DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo ResolveTaipeiTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        }
    }
}

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
