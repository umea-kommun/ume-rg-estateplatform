using System.Diagnostics.CodeAnalysis;
using Cronos;

namespace Umea.se.EstateService.Logic.Sync;

public static class CronHelper
{
    public static bool TryParse(
        string? schedule,
        string timeZoneId,
        [NotNullWhen(true)] out CronExpression? expression,
        [NotNullWhen(true)] out TimeZoneInfo? timeZone)
    {
        if (string.IsNullOrWhiteSpace(schedule))
        {
            expression = null;
            timeZone = null;
            return false;
        }

        try
        {
            expression = CronExpression.Parse(schedule);
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (Exception ex) when (ex is CronFormatException or TimeZoneNotFoundException)
        {
            expression = null;
            timeZone = null;
            return false;
        }
    }

    public static CronExpression? ParseOrNull(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        try
        {
            return CronExpression.Parse(expression);
        }
        catch (CronFormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns true if the next cron occurrence after <paramref name="lastRefresh"/> is in the past,
    /// meaning a scheduled run was missed.
    /// </summary>
    public static bool IsOverdue(DateTimeOffset? lastRefresh, CronExpression cron, TimeZoneInfo tz)
    {
        if (!lastRefresh.HasValue)
        {
            return false;
        }

        DateTimeOffset? nextAfterLast = cron.GetNextOccurrence(lastRefresh.Value, tz);
        return nextAfterLast.HasValue && nextAfterLast.Value <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Returns the first scheduled run after <paramref name="lastSuccess"/> if it should have
    /// completed by <paramref name="utcNow"/>, or null if no run has been missed.
    /// </summary>
    public static DateTimeOffset? GetMissedRun(
        DateTimeOffset lastSuccess, CronExpression cron, TimeZoneInfo tz, DateTimeOffset utcNow)
    {
        // The scheduler can fire a moment before the cron time, so a run that started at 00:59:59
        // must count as the 01:00 run rather than leaving that occurrence looking missed.
        TimeSpan earlyStartTolerance = TimeSpan.FromMinutes(5);
        TimeSpan runDuration = TimeSpan.FromHours(1);

        DateTimeOffset? nextRun = cron.GetNextOccurrence(lastSuccess + earlyStartTolerance, tz);
        return nextRun.HasValue && nextRun.Value + runDuration <= utcNow ? nextRun : null;
    }
}
