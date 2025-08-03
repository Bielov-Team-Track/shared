using System.Security.Claims;

namespace Shared.DataAccess.Providers.Interfaces
{
    public interface IJwtPayloadProvider
    {
        JwtPayloadDto GetJwtPayload(IEnumerable<Claim> claims, string accessToken);
    }
}
