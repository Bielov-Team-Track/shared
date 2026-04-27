using Shared.Enums;

namespace Shared.Exceptions
{
    public class EntityNotFoundException : ExceptionWithStatusAndErrorCodes
    {
        public EntityNotFoundException(string message)
            : base(message, System.Net.HttpStatusCode.NotFound, ErrorCodeEnum.EntityNotFound)
        {
        }

        public EntityNotFoundException(string message, ErrorCodeEnum errorCode)
            : base(message, System.Net.HttpStatusCode.NotFound, errorCode)
        {
        }
    }
}
