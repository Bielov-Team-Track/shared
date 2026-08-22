using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Guardian.Interfaces;
using Shared.Guardian.Models;

namespace Shared.Guardian.Hosting;

/// <summary>
/// Push fan-out is on nobody's request path, so a dropped revoke would otherwise heal only
/// when the guardian next made a request. This sweeps every guardian holding a link.
/// </summary>
public class GuardianLinkPruneService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GuardianLinkPruneService> _logger;
    private readonly TimeProvider _timeProvider;

    public GuardianLinkPruneService(IServiceScopeFactory scopeFactory,
        ILogger<GuardianLinkPruneService> logger,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, _timeProvider, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await SweepAsync(stoppingToken);
                await Task.Delay(Interval, _timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task SweepAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var linkRepository = scope.ServiceProvider.GetRequiredService<IRepository<GuardianLink>>();
            var linkService = scope.ServiceProvider.GetRequiredService<IGuardianLinkService>();

            var guardianIds = await linkRepository.QueryNoTracking()
                .Select(l => l.GuardianUserId)
                .Distinct()
                .ToListAsync(stoppingToken);

            var reconciled = 0;
            var failed = 0;
            foreach (var guardianId in guardianIds)
            {
                if (stoppingToken.IsCancellationRequested) return;

                try
                {
                    await linkService.EnsureFreshAsync(guardianId, force: true);
                    reconciled++;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "Guardian link prune failed for {GuardianId}; continuing sweep",
                        guardianId);
                }
            }

            if (guardianIds.Count > 0)
                _logger.LogInformation(
                    "Guardian link prune complete: {Reconciled} of {Guardians} guardians, {Failed} failed",
                    reconciled, guardianIds.Count, failed);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guardian link prune sweep failed; the next pass will retry");
        }
    }
}
