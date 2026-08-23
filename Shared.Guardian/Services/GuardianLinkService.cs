using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Guardian.Interfaces;
using Shared.Guardian.Models;
using Shared.Models;
using Shared.Services.Services.Interfaces;

namespace Shared.Guardian.Services;

public class GuardianLinkService : IGuardianLinkService
{
    private static readonly TimeSpan MarkerTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan FailureMarkerTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ForcedMarkerTtl = TimeSpan.FromSeconds(60);

    private readonly IRepository<GuardianLink> _linkRepository;
    private readonly IRepository<UserProfile> _userProfileRepository;
    private readonly IGuardianLinkSource _source;
    private readonly IDistributedCache _cache;
    private readonly IAgeTierService _ageTierService;
    private readonly ILogger<GuardianLinkService> _logger;

    public GuardianLinkService(
        IRepository<GuardianLink> linkRepository,
        IRepository<UserProfile> userProfileRepository,
        IGuardianLinkSource source,
        IDistributedCache cache,
        IAgeTierService ageTierService,
        ILogger<GuardianLinkService> logger)
    {
        _linkRepository = linkRepository;
        _userProfileRepository = userProfileRepository;
        _source = source;
        _cache = cache;
        _ageTierService = ageTierService;
        _logger = logger;
    }

    private static string MarkerKey(Guid userId) => $"guardian-links-verified:{userId}";

    private static string ForcedMarkerKey(Guid userId) => $"guardian-links-forced:{userId}";

    private static string WardMarkerKey(Guid wardId) => $"guardian-links-ward-verified:{wardId}";

    private static string WardForcedMarkerKey(Guid wardId) => $"guardian-links-ward-forced:{wardId}";

    public async Task UpsertAsync(Guid guardianId, Guid wardId, GuardianPermission permissions,
        GuardianTier tier = GuardianTier.Guardian)
    {
        var existing = await _linkRepository.Query()
            .FirstOrDefaultAsync(l => l.GuardianUserId == guardianId && l.WardUserId == wardId);
        if (existing == null)
        {
            _linkRepository.Add(new GuardianLink
            {
                GuardianUserId = guardianId,
                WardUserId = wardId,
                Permissions = permissions,
                Tier = tier
            });
        }
        else
        {
            if (existing.Permissions == permissions && existing.Tier == tier) return;
            existing.Permissions = permissions;
            existing.Tier = tier;
            _linkRepository.Update(existing);
        }
        await _linkRepository.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid guardianId, Guid wardId)
    {
        var existing = await _linkRepository.Query()
            .FirstOrDefaultAsync(l => l.GuardianUserId == guardianId && l.WardUserId == wardId);
        if (existing == null) return;
        _linkRepository.Delete(existing);
        await _linkRepository.SaveChangesAsync();
    }

    public async Task RemoveAllForWardAsync(Guid wardId)
    {
        var links = await _linkRepository.Query().Where(l => l.WardUserId == wardId).ToListAsync();
        if (links.Count == 0) return;
        foreach (var link in links) _linkRepository.Delete(link);
        await _linkRepository.SaveChangesAsync();
    }

    public async Task RemoveAllForUserAsync(Guid userId)
    {
        var links = await _linkRepository.Query()
            .Where(l => l.WardUserId == userId || l.GuardianUserId == userId).ToListAsync();
        if (links.Count == 0) return;
        foreach (var link in links) _linkRepository.Delete(link);
        await _linkRepository.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetWardIdsAsync(Guid guardianId,
        GuardianPermission required = GuardianPermission.None)
    {
        return await LinksFor(guardianId, required)
            .Select(l => l.WardUserId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetMinorWardIdsAsync(Guid guardianId,
        GuardianPermission required = GuardianPermission.None)
    {
        var candidates = await (
            from l in LinksFor(guardianId, required)
            join u in _userProfileRepository.Query() on l.WardUserId equals u.Id
            where u.DateOfBirth != null
            select new { l.WardUserId, u.DateOfBirth })
            .ToListAsync();

        return candidates
            .Where(c => _ageTierService.IsMinor(c.DateOfBirth!.Value))
            .Select(c => c.WardUserId)
            .Distinct()
            .ToList();
    }

    private IQueryable<GuardianLink> LinksFor(Guid guardianId, GuardianPermission required)
    {
        var links = _linkRepository.Query().Where(l => l.GuardianUserId == guardianId);
        return required == GuardianPermission.None
            ? links
            : links.Where(l => (l.Permissions & required) == required);
    }

    public async Task EnsureFreshAsync(Guid userId, bool force = false)
    {
        var gate = force ? ForcedMarkerKey(userId) : MarkerKey(userId);
        if (await IsMarkerPresentAsync(gate, userId)) return;

        try
        {
            var remoteMinors = (await _source.GetMinorsForGuardianAsync(userId))
                .Distinct().ToList();

            var localLinks = await _linkRepository.Query()
                .Where(l => l.GuardianUserId == userId).ToListAsync();
            var localByWard = localLinks.ToDictionary(l => l.WardUserId);

            // Independent read-only lookups — CheckGuardianAccessAsync already catches
            // RpcException per-call and returns null, so this is safe to parallelize.
            var accessInfos = await Task.WhenAll(
                remoteMinors.Select(minorId => _source.CheckGuardianAccessAsync(userId, minorId)));

            var deniedWardIds = new HashSet<Guid>();

            for (var i = 0; i < remoteMinors.Count; i++)
            {
                var minorId = remoteMinors[i];
                var info = accessInfos[i];

                // RPC failure for this one ward — leave any existing link untouched rather
                // than risk revoking access on a transient per-call error.
                if (info is null) continue;

                if (!info.HasAccess)
                {
                    deniedWardIds.Add(minorId);
                    continue;
                }

                if (localByWard.TryGetValue(minorId, out var existing))
                {
                    if (existing.Permissions == info.Permissions && existing.Tier == info.Tier) continue;
                    existing.Permissions = info.Permissions;
                    existing.Tier = info.Tier;
                    _linkRepository.Update(existing);
                }
                else
                {
                    _linkRepository.Add(new GuardianLink
                    {
                        GuardianUserId = userId,
                        WardUserId = minorId,
                        Permissions = info.Permissions,
                        Tier = info.Tier
                    });
                }
            }

            var remoteSet = remoteMinors.ToHashSet();
            foreach (var link in localLinks.Where(l =>
                         !remoteSet.Contains(l.WardUserId) || deniedWardIds.Contains(l.WardUserId)))
                _linkRepository.Delete(link);

            await _linkRepository.SaveChangesAsync();
            await SetMarkersAsync(userId, MarkerTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guardian link reconcile skipped for {UserId}: profiles unavailable", userId);
            await SetMarkersAsync(userId, FailureMarkerTtl);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetGuardianIdsForWardAsync(Guid wardUserId)
    {
        return await _linkRepository.Query()
            .Where(l => l.WardUserId == wardUserId)
            .Select(l => l.GuardianUserId)
            .Distinct()
            .ToListAsync();
    }

    public async Task EnsureWardFreshAsync(Guid wardUserId, bool force = false)
    {
        var gate = force ? WardForcedMarkerKey(wardUserId) : WardMarkerKey(wardUserId);
        if (await IsMarkerPresentAsync(gate, wardUserId)) return;

        try
        {
            var remoteGuardians = (await _source.GetGuardiansForMinorAsync(wardUserId))
                .Distinct().ToList();

            var localLinks = await _linkRepository.Query()
                .Where(l => l.WardUserId == wardUserId).ToListAsync();
            var localGuardians = localLinks.Select(l => l.GuardianUserId).ToHashSet();

            /*
             * View, and only for a guardian we have no row for. GetGuardiansForMinorResponse
             * carries ids and no permissions, so writing over an existing row would flatten a
             * Pay grant to View every time a people list rendered. View is the floor: it answers
             * the facet and grants nothing else until a grant event or EnsureFreshAsync — which
             * does carry permissions — corrects it. The tier is seeded the same way and for the
             * same reason: the response carries none, so a written tier would promote every
             * Contact to a full guardian on every render. GuardianTier.Guardian is its floor.
             */
            foreach (var guardianId in remoteGuardians.Where(g => !localGuardians.Contains(g)))
                _linkRepository.Add(new GuardianLink
                {
                    GuardianUserId = guardianId,
                    WardUserId = wardUserId,
                    Permissions = GuardianPermission.View,
                    Tier = GuardianTier.Guardian
                });

            var remoteSet = remoteGuardians.ToHashSet();
            foreach (var link in localLinks.Where(l => !remoteSet.Contains(l.GuardianUserId)))
                _linkRepository.Delete(link);

            await _linkRepository.SaveChangesAsync();
            await SetWardMarkersAsync(wardUserId, MarkerTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guardian link ward reconcile skipped for {WardId}: profiles unavailable",
                wardUserId);
            await SetWardMarkersAsync(wardUserId, FailureMarkerTtl);
        }
    }

    private async Task<bool> IsMarkerPresentAsync(string key, Guid userId)
    {
        try
        {
            return await _cache.GetAsync(key) != null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Guardian marker cache read failed for {UserId}; treating as absent", userId);
            return false;
        }
    }

    private Task SetMarkersAsync(Guid userId, TimeSpan verifiedTtl) =>
        SetMarkerPairAsync(MarkerKey(userId), ForcedMarkerKey(userId), verifiedTtl, userId);

    private Task SetWardMarkersAsync(Guid wardId, TimeSpan verifiedTtl) =>
        SetMarkerPairAsync(WardMarkerKey(wardId), WardForcedMarkerKey(wardId), verifiedTtl, wardId);

    private Task SetMarkerPairAsync(string verifiedKey, string forcedKey, TimeSpan verifiedTtl, Guid userId) =>
        Task.WhenAll(
            SetMarkerAsync(verifiedKey, verifiedTtl, userId),
            SetMarkerAsync(forcedKey, ForcedMarkerTtl, userId));

    private async Task SetMarkerAsync(string key, TimeSpan ttl, Guid userId)
    {
        try
        {
            await _cache.SetAsync(key, "1"u8.ToArray(),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Guardian marker cache write failed for {UserId}", userId);
        }
    }
}
