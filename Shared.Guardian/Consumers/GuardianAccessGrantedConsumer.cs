using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Guardian.Interfaces;
using Shared.Messaging.Contracts.Events.Family;

namespace Shared.Guardian.Consumers;

public class GuardianAccessGrantedConsumer : IConsumer<GuardianAccessGrantedEvent>
{
    private readonly IGuardianLinkService _guardianLinkService;
    private readonly ILogger<GuardianAccessGrantedConsumer> _logger;

    public GuardianAccessGrantedConsumer(IGuardianLinkService guardianLinkService,
        ILogger<GuardianAccessGrantedConsumer> logger)
    {
        _guardianLinkService = guardianLinkService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GuardianAccessGrantedEvent> context)
    {
        var msg = context.Message;
        await _guardianLinkService.UpsertAsync(msg.GuardianId, msg.MinorId, msg.Permissions, msg.Tier);
        _logger.LogInformation("Guardian link upserted: {GuardianId} -> {MinorId} ({Tier}, {Permissions})",
            msg.GuardianId, msg.MinorId, msg.Tier, msg.Permissions);
    }
}
