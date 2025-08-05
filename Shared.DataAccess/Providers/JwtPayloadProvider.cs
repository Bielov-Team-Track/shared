using System.Security.Claims;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Models.Jwt;

namespace Shared.DataAccess.Providers
{
    public class JwtPayloadProvider : IJwtPayloadProvider
    {
        public JwtPayloadDto GetJwtPayload(IEnumerable<Claim> claims, string accessToken)
        {
            Guid? userId;

            try
            {
                var userIdClaim = claims?.FirstOrDefault(x => x.Type == "UserId")?.Value;

                userId = userIdClaim == null ? null : Guid.Parse(userIdClaim);
            }
            catch
            {
                throw new ArgumentException("ApiToken has wrong JWT format");
            }

            accessToken = accessToken.Replace("Bearer ", "");

            return new JwtPayloadDto
            {
                AccessToken = accessToken,
                UserId = userId
            };
        }
    }
}
