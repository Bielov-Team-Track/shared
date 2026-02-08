using MassTransit;
using Auth.Application.Interfaces.Repositories;
using Auth.Domain.Models;
using Shared.Messaging.Contracts.Events.Family;

namespace Shared.Messaging.Consumers;

public class MinorCredentialsGrantedConsumer : IConsumer<MinorCredentialsGrantedEvent>
{
    private readonly IUserRepository _userRepository;

    public MinorCredentialsGrantedConsumer(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Consume(ConsumeContext<MinorCredentialsGrantedEvent> context)
    {
        var message = context.Message;

        // Check if user already exists
        var existingUser = await _userRepository.GetByIdAsync(message.MinorUserId);
        if (existingUser != null)
        {
            // User already exists, skip creation
            return;
        }

        // Create managed user record
        var managedUser = new User
        {
            Id = message.MinorUserId,
            Email = message.Email,
            PasswordHash = string.Empty, // No password until account is claimed
            IsActive = true,
            IsEmailVerified = false,
            IsManaged = true,
            ManagedByHouseholdId = null // Will be set when we have household context
        };

        _userRepository.Add(managedUser);
        await _userRepository.SaveChangesAsync();
    }
}
