using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Microservices.Controllers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseApiController<T> : ControllerBase
    {
        private readonly IJwtPayloadProvider<T> _jwtPayloadProvider;

        private JwtPayloadDto _jwtPayload;

        protected BaseApiController(IJwtPayloadProvider<T> jwtPayloadProvider)
        {
            _jwtPayloadProvider = jwtPayloadProvider;
        }

        protected JwtPayloadDto JwtPayload => _jwtPayload ??= GetJwtPayload();

        protected void CheckIsUserLoggedIn()
        {
            if (string.IsNullOrEmpty(JwtPayload.LoyaltyId))
            {
                throw new BadRequestException("Only logged in users have access to this functionality. Please, login.",
                ErrorCodeEnum.ValidationError);
            }
        }

        private JwtPayloadDto GetJwtPayload()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var jwtString = GetJwtFromHeader(HttpContext.Request);
            var jwtPayload = _jwtPayloadProvider.GetJwtPayload(identity?.Claims, jwtString);

            return jwtPayload;
        }

        private static string GetJwtFromHeader(HttpRequest request)
        {
            if (request.Headers.TryGetValue("Authorization", out var headers))
            {
                return headers[0];
            }

            return string.Empty;
        }
    }
}
