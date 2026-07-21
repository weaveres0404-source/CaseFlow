namespace CaseFlow.Server.Helpers;

/// <summary>
/// Centralizes application time handling.
/// Event timestamps retain DateTimeKind.Utc so Npgsql writes the same instant
/// regardless of whether the application runs locally or on Cloud Run.
/// </summary>
public static class TimeHelper
{
    private static readonly TimeZoneInfo TaipeiTimeZone = ResolveTaipeiTimeZone();

    public static DateTime Now => DateTime.UtcNow;

    public static DateTime TaipeiNow =>
        DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTimeZone),
            DateTimeKind.Unspecified);

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
