using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Grpc;
using Shared.Contracts.Interfaces;
using Shared.Contracts.Models;

namespace Shared.Services.UserProfile;

public class UserProfileGrpcClient : IUserProfileGrpcService, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly UserProfileService.UserProfileServiceClient _client;
    private readonly ILogger<UserProfileGrpcClient> _logger;

    public UserProfileGrpcClient(string serviceUrl, ILogger<UserProfileGrpcClient> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(serviceUrl);
        _client = new UserProfileService.UserProfileServiceClient(_channel);
    }

    public async Task<CreateUserProfileResponse> CreateUserProfileAsync(CreateUserProfileRequest request)
    {
        try
        {
            _logger.LogInformation("Sending gRPC request to create profile for user {UserId}", request.UserId);

            var grpcRequest = new CreateProfileRequest
            {
                UserId = request.UserId.ToString(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var grpcResponse = await _client.CreateProfileAsync(grpcRequest);

            _logger.LogInformation("Received gRPC response for user {UserId}: Success={Success}", 
                request.UserId, grpcResponse.Success);

            return new CreateUserProfileResponse
            {
                Success = grpcResponse.Success,
                Message = grpcResponse.Message,
                ProfileId = grpcResponse.ProfileId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC call failed for user {UserId}", request.UserId);
            return new CreateUserProfileResponse
            {
                Success = false,
                Message = $"gRPC call failed: {ex.Message}"
            };
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        GC.SuppressFinalize(this);
    }
}