using System.Text.RegularExpressions;
using CaseFlow.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Helpers;

public static class GoogleUserProfileSync
{
    public static async Task SyncAsync(
        CaseFlowDbContext db,
        User user,
        string googleSub,
        string googleEmail,
        string? googleName,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var changed = false;

        if (user.GoogleSub != googleSub) { user.GoogleSub = googleSub; changed = true; }
        if (user.GoogleEmail != googleEmail) { user.GoogleEmail = googleEmail; changed = true; }

        if (string.IsNullOrWhiteSpace(user.Username))
        {
            user.Username = await GenerateUniqueUsernameAsync(db, googleName, googleEmail, user.UserId, cancellationToken);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(googleName))
        {
            user.FullName = googleName.Trim();
            changed = true;
        }

        if (changed)
            user.UpdatedAt = now;

        user.LastLoginAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string> GenerateUniqueUsernameAsync(
        CaseFlowDbContext db,
        string? googleName,
        string googleEmail,
        int userId,
        CancellationToken cancellationToken)
    {
        var baseName = NormalizeUsernameCandidate(googleName);

        if (string.IsNullOrWhiteSpace(baseName))
            baseName = NormalizeUsernameCandidate(googleEmail.Split('@')[0]);

        if (string.IsNullOrWhiteSpace(baseName))
            baseName = $"google-user-{userId}";

        var candidate = baseName;
        var suffix = 2;

        while (await db.Users.AnyAsync(u => u.UserId != userId && u.Username == candidate, cancellationToken))
        {
            var suffixText = $"-{suffix}";
            var maxBaseLength = Math.Max(1, 50 - suffixText.Length);
            candidate = TrimToLength(baseName, maxBaseLength) + suffixText;
            suffix++;
        }

        return candidate;
    }

    private static string NormalizeUsernameCandidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = Regex.Replace(value.Trim(), "\\s+", " ");
        return TrimToLength(normalized, 50);
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength].TrimEnd();
    }
}
