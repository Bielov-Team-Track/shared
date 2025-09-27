using Shared.Enums;

namespace Shared.Exceptions
{
    public class EntityNotFoundException : BadRequestException
    {
        public EntityNotFoundException(string message, ErrorCodeEnum errorCode) : base(message, errorCode)
        {
        }
    }
}
