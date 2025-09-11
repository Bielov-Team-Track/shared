using Shared.Contracts.Models;

namespace Shared.Contracts.Interfaces;

public interface IUserProfileGrpcService
{
    Task<CreateUserProfileResponse> CreateUserProfileAsync(CreateUserProfileRequest request);
}