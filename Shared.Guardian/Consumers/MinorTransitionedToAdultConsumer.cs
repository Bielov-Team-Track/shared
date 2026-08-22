using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Guardian.Interfaces;
using Shared.Messaging.Contracts.Events.Family;

namespace Shared.Guardian.Consumers;

public class MinorTransitionedToAdultConsumer : IConsumer<MinorTransitionedToAdultEvent>
{
    private readonly IGuardianLinkService _guardianLinkService;
    private readonly ILogger<MinorTransitionedToAdultConsumer> _logger;

    public MinorTransitionedToAdultConsumer(IGuardianLinkService guardianLinkService,
        ILogger<MinorTransitionedToAdultConsumer> logger)
    {
        _guardianLinkService = guardianLinkService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MinorTransitionedToAdultEvent> context)
    {
        var msg = context.Message;
        await _guardianLinkService.RemoveAllForWardAsync(msg.UserId);
        _logger.LogInformation("Purged all guardian links for user {UserId} transitioning to adult", msg.UserId);
    }
}
