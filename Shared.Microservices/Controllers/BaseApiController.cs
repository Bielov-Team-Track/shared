using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Models.Jwt;

namespace Shared.Microservices.Controllers
{
    [ExcludeFromCodeCoverage]
    [ApiController]
    [Route("v{version:apiVersion}/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        private readonly IJwtPayloadProvider _jwtPayloadProvider;

        private JwtPayloadDto? _jwtPayload;

        protected JwtPayloadDto? JwtPayload => _jwtPayload ??= GetJwtPayload();

        protected BaseApiController(IJwtPayloadProvider jwtPayloadProvider)
        {
            _jwtPayloadProvider = jwtPayloadProvider;
        }

        protected void CheckIsUserLoggedIn()
        {
            if (JwtPayload == null || JwtPayload.UserId == Guid.Empty)
            {
                throw new UnauthorizedException("Only logged in users have access to this functionality. Please, login.",
                ErrorCodeEnum.Unauthorized);
            }
        }

        private JwtPayloadDto? GetJwtPayload()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null || identity.Claims == null)
            {
                return null;
            }

            var jwtString = GetJwtFromHeader(HttpContext.Request);
            var jwtPayload = _jwtPayloadProvider.GetJwtPayload(identity.Claims, jwtString);

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
