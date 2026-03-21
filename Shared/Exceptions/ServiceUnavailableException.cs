using Shared.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Shared.Exceptions
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class ServiceUnavailableException : ExceptionWithStatusAndErrorCodes
    {
        public ServiceUnavailableException(string message)
            : base(message, HttpStatusCode.ServiceUnavailable, ErrorCodeEnum.ServiceUnavailable)
        {
        }

        public ServiceUnavailableException(string message, Exception innerException)
            : base(message, innerException, HttpStatusCode.ServiceUnavailable, ErrorCodeEnum.ServiceUnavailable)
        {
        }
    }
}
