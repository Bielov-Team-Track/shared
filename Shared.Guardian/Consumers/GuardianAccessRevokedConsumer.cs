using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Guardian.Interfaces;
using Shared.Messaging.Contracts.Events.Family;

namespace Shared.Guardian.Consumers;

public class GuardianAccessRevokedConsumer : IConsumer<GuardianAccessRevokedEvent>
{
    private readonly IGuardianLinkService _guardianLinkService;
    private readonly ILogger<GuardianAccessRevokedConsumer> _logger;

    public GuardianAccessRevokedConsumer(IGuardianLinkService guardianLinkService,
        ILogger<GuardianAccessRevokedConsumer> logger)
    {
        _guardianLinkService = guardianLinkService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GuardianAccessRevokedEvent> context)
    {
        var msg = context.Message;
        await _guardianLinkService.RemoveAsync(msg.GuardianId, msg.MinorId);
        _logger.LogInformation("Guardian link removed: {GuardianId} -> {MinorId}",
            msg.GuardianId, msg.MinorId);
    }
}
