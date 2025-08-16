using Shared.Contracts.Models;
using Shared.Contracts.Grpc;

namespace Shared.Contracts.Interfaces;

public interface IUserProfileGrpcService
{
    Task<CreateUserProfileResponse> CreateUserProfileAsync(CreateUserProfileRequest request);
}