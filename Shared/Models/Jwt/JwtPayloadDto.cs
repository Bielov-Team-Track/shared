namespace Shared.Models.Jwt
{
    public class JwtPayloadDto
    {
        public Guid? UserId { get; set; }
        public string? AccessToken { get; init; }
    }
}
