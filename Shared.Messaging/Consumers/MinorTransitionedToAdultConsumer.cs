using MassTransit;
using Auth.Application.Interfaces.Repositories;
using Shared.Messaging.Contracts.Events.Family;

namespace Shared.Messaging.Consumers;

public class MinorTransitionedToAdultConsumer : IConsumer<MinorTransitionedToAdultEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public MinorTransitionedToAdultConsumer(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Consume(ConsumeContext<MinorTransitionedToAdultEvent> context)
    {
        var message = context.Message;

        // Get the user
        var user = await _userRepository.GetByIdAsync(message.UserId);
        if (user == null)
        {
            // User not found, nothing to do
            return;
        }

        // Invalidate all refresh tokens for this user (force re-login)
        await _refreshTokenRepository.RevokeAllUserTokensAsync(message.UserId);

        // Update IsManaged flag
        user.IsManaged = false;
        user.ManagedByHouseholdId = null;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();
    }
}
