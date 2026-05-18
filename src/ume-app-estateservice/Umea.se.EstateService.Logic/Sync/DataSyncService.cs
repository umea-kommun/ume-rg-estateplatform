using System.Threading.Channels;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.Sync;

/// <summary>
/// Background service that orchestrates data sync scheduling and manual triggers.
/// All data pipeline logic lives in <see cref="RefreshPipelineRunner"/>.
/// </summary>
public sealed class DataSyncService(
    IDataStore dataStore,
    DocumentSyncHandler documentSyncHandler,
    RefreshPipelineRunner refreshPipeline,
    ApplicationConfig appConfig,
    ILogger<DataSyncService> logger) : BackgroundService
{
    private readonly DataSyncConfiguration _options = appConfig.DataSync;
    private readonly Channel<RefreshTrigger> _triggerQueue =
        Channel.CreateUnbounded<RefreshTrigger>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private int _isRefreshing;
    private int _hasPendingTrigger;
    private DateTimeOffset? _nextRefreshTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string version = typeof(DataSyncService).Assembly.GetName().Version?.ToString() ?? "unknown";
        string informationalVersion = System.Reflection.CustomAttributeExtensions
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(DataSyncService).Assembly)?.InformationalVersion ?? "unknown";
        logger.LogInformation("DataSyncService starting. Assembly version: {Version}, Informational: {InformationalVersion}",
            version, informationalVersion);

        (bool loadedFromCache, DateTimeOffset? lastRefresh) =
            await refreshPipeline.TryRestoreFromCacheAsync(stoppingToken);

        bool hasCron = CronHelper.TryParse(
            _options.Schedule.Default,
            _options.TimeZone,
            out CronExpression? cron,
            out TimeZoneInfo? tz);

        if (!hasCron && !string.IsNullOrWhiteSpace(_options.Schedule.Default))
        {
            logger.LogError("Invalid cron expression '{Schedule}'. Scheduled refresh disabled.", _options.Schedule.Default);
        }

        if (!loadedFromCache)
        {
            await RunStartupRefreshWithRetriesAsync(stoppingToken).ConfigureAwait(false);
        }
        else if (hasCron && CronHelper.IsOverdue(lastRefresh, cron!, tz!))
        {
            logger.LogInformation("Last refresh was {Ago} ago — running catch-up refresh.",
                DateTimeOffset.UtcNow - lastRefresh);
            await RunCatchUpRefreshAsync(stoppingToken).ConfigureAwait(false);
        }

        if (hasCron)
        {
            RunInBackground(RunScheduledRefreshAsync(cron!, tz!, stoppingToken), "Refresh loop", stoppingToken);
        }
        else if (string.IsNullOrWhiteSpace(_options.Schedule.Default))
        {
            logger.LogInformation("Scheduled refresh disabled (no Schedule configured). Manual triggers are still allowed.");
        }

        RunInBackground(
            RunSupplementarySyncLoopsAsync(stoppingToken),
            "Supplementary sync loops",
            stoppingToken);

        // Manual trigger queue
        try
        {
            while (await _triggerQueue.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    while (_triggerQueue.Reader.TryRead(out RefreshTrigger trigger))
                    {
                        await RunRefreshOwnedAsync(trigger.Source, stoppingToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _hasPendingTrigger, 0);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("DataSyncService stopping.");
        }
    }

    // ── Public API ───────────────────────────────────────────────────

    public Task<RefreshStatus> TriggerManualRefreshAsync()
    {
        if (IsBusy())
        {
            return Task.FromResult(RefreshStatus.AlreadyRunning);
        }

        return Task.FromResult(TryQueueTrigger(RefreshTriggerSource.Manual)
            ? RefreshStatus.Started
            : RefreshStatus.AlreadyRunning);
    }

    public RefreshStatus TriggerDocumentSync()
    {
        return documentSyncHandler.TryTriggerSync()
            ? RefreshStatus.Started
            : RefreshStatus.AlreadyRunning;
    }

    public DataStoreInfo GetDataStoreInfo()
    {
        return new DataStoreInfo
        {
            EstateCount = dataStore.Estates.Count(),
            BuildingCount = dataStore.Buildings.Count(),
            FloorCount = dataStore.Floors.Count(),
            RoomCount = dataStore.Rooms.Count(),
            IsReady = dataStore.IsReady,
            LastRefreshTime = dataStore.LastRefreshUtc,
            LastAttemptTime = dataStore.LastAttemptUtc,
            NextRefreshTime = _nextRefreshTime,
            RefreshSchedule = _options.Schedule.Default,
            DocumentSyncSchedule = _options.Schedule.Resolve(SyncType.Documents),
            IsRefreshing = IsBusy()
        };
    }

    // ── Core refresh scheduling ──────────────────────────────────────

    private async Task<bool> RunStartupRefreshWithRetriesAsync(CancellationToken cancellationToken)
    {
        int maxAttempts = _options.MaxRetries + 1;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt > 1)
            {
                int delaySeconds = _options.RetryBaseDelaySeconds * (1 << (attempt - 2));
                delaySeconds = Math.Min(delaySeconds, 300);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
            }

            bool refreshSucceeded = await RunRefreshOwnedAsync(RefreshTriggerSource.Startup, cancellationToken)
                .ConfigureAwait(false);

            if (refreshSucceeded)
            {
                return true;
            }
        }

        logger.LogCritical("Startup refresh failed after {Attempts} attempts.", maxAttempts);
        return false;
    }

    private Task<bool> RunCatchUpRefreshAsync(CancellationToken cancellationToken)
        => RunRefreshOwnedAsync(RefreshTriggerSource.Startup, cancellationToken);

    private Task RunScheduledRefreshAsync(CronExpression cron, TimeZoneInfo tz, CancellationToken ct)
    {
        return RunOnCronAsync("Refresh", cron, tz, _ =>
        {
            if (!TryQueueTrigger(RefreshTriggerSource.Scheduled))
            {
                logger.LogInformation("Skipping scheduled trigger because a refresh is active or queued.");
            }
            return Task.CompletedTask;
        }, setNextRefreshTime: true, ct);
    }

    private async Task<bool> RunRefreshOwnedAsync(RefreshTriggerSource source, CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _isRefreshing, 1);

        try
        {
            using IDisposable? scope = logger.BeginScope("Refresh source: {Source}", source);
            return await refreshPipeline.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _isRefreshing, 0);
        }
    }

    // ── Supplementary syncs ──────────────────────────────────────────

    private async Task RunSupplementarySyncLoopsAsync(CancellationToken ct)
    {
        await dataStore.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);

        (string Name, SyncType Type, Func<CancellationToken, Task> Action)[] syncs =
        {
            ("Documents", SyncType.Documents, documentSyncHandler.SyncAllBuildingsAsync),
        };

        List<Task> loops = [];
        foreach ((string name, SyncType type, Func<CancellationToken, Task> action) in syncs)
        {
            CronExpression? cron = CronHelper.ParseOrNull(_options.Schedule.Resolve(type));
            if (cron is null)
            {
                logger.LogInformation("{SyncName} sync disabled (no schedule).", name);
                continue;
            }
            loops.Add(RunSyncLoopAsync(name, action, cron, tz, ct));
        }

        await Task.WhenAll(loops).ConfigureAwait(false);
    }

    private async Task RunSyncLoopAsync(
        string name, Func<CancellationToken, Task> action,
        CronExpression cron, TimeZoneInfo tz, CancellationToken ct)
    {
        logger.LogInformation("{SyncName} sync loop started. Running initial sync.", name);
        try
        {
            await action(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{SyncName} sync failed.", name);
        }

        await RunOnCronAsync(name, cron, tz, action, setNextRefreshTime: false, ct).ConfigureAwait(false);
    }

    private async Task RunOnCronAsync(
        string name, CronExpression cron, TimeZoneInfo tz,
        Func<CancellationToken, Task> action, bool setNextRefreshTime, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                DateTimeOffset? nextUtc = cron.GetNextOccurrence(utcNow, tz);

                if (nextUtc is null)
                {
                    logger.LogWarning("{Name} cron has no future occurrences. Loop stopped.", name);
                    return;
                }

                if (setNextRefreshTime)
                {
                    _nextRefreshTime = nextUtc;
                }

                logger.LogInformation("{Name} next run at {NextRun:u} (in {Delay}).",
                    name, nextUtc.Value, nextUtc.Value - utcNow);

                await Task.Delay(nextUtc.Value - utcNow, ct).ConfigureAwait(false);

                try
                {
                    await action(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "{Name} failed.", name);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal shutdown
        }
    }

    // ── Trigger queue ────────────────────────────────────────────────

    private bool TryQueueTrigger(RefreshTriggerSource source)
    {
        if (Interlocked.CompareExchange(ref _hasPendingTrigger, 1, 0) != 0)
        {
            return false;
        }

        if (!_triggerQueue.Writer.TryWrite(new RefreshTrigger(source, DateTimeOffset.UtcNow)))
        {
            Interlocked.Exchange(ref _hasPendingTrigger, 0);
            return false;
        }

        return true;
    }

    private bool IsBusy()
        => Volatile.Read(ref _isRefreshing) == 1 || Volatile.Read(ref _hasPendingTrigger) == 1;

    private void RunInBackground(Task task, string name, CancellationToken ct)
        => _ = LogBackgroundTaskFailureAsync(task, name, ct);

    private async Task LogBackgroundTaskFailureAsync(Task task, string name, CancellationToken ct)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogDebug("{Name} stopped.", name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Name} stopped unexpectedly.", name);
        }
    }

    private enum RefreshTriggerSource
    {
        Startup,
        Scheduled,
        Manual
    }

    private readonly record struct RefreshTrigger(RefreshTriggerSource Source, DateTimeOffset RequestedAtUtc);
}
