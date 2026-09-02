using Microsoft.AspNetCore.Http;

namespace Shared.Services.Analytics;

public sealed class ClientSourceAccessor : IClientSourceAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientSourceAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Current =>
        ClientSource.Normalize(_httpContextAccessor.HttpContext?.Request.Headers[ClientSource.HeaderName]);
}
