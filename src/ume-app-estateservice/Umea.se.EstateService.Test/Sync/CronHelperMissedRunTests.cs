using System.Globalization;
using Cronos;
using Umea.se.EstateService.Logic.Sync;

namespace Umea.se.EstateService.Test.Sync;

public class CronHelperMissedRunTests
{
    private static readonly CronExpression DailyAt3 = CronExpression.Parse("0 3 * * *");
    private static readonly TimeZoneInfo Stockholm = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    private static DateTimeOffset Utc(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    [Theory]
    [InlineData("2026-10-21T00:59:59Z", "2026-10-21T12:00:00Z")]
    [InlineData("2026-10-21T00:59:59Z", "2026-10-22T01:30:00Z")]
    [InlineData("2026-10-22T00:59:59Z", "2026-10-22T02:30:00Z")]
    [InlineData("2026-10-21T14:00:00Z", "2026-10-22T01:59:00Z")]
    public void GetMissedRun_LatestRunPersisted_ReturnsNull(string lastSuccess, string now)
    {
        CronHelper.GetMissedRun(Utc(lastSuccess), DailyAt3, Stockholm, Utc(now)).ShouldBeNull();
    }

    [Fact]
    public void GetMissedRun_NightlyPersistFailed_ReturnsMissedRun()
    {
        DateTimeOffset? missedRun = CronHelper.GetMissedRun(
            Utc("2026-10-21T00:59:59Z"), DailyAt3, Stockholm, Utc("2026-10-22T02:30:00Z"));

        missedRun.ShouldBe(Utc("2026-10-22T01:00:00Z"));
    }
}
