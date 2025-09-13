using Shared.Services.Interfaces;

namespace Shared.Services
{
    public class EmailService : IEmailService
    {
        public Task SendEmailAsync(string email, string templateName, Dictionary<string, string> parameters)
        {
            throw new NotImplementedException();
        }
    }
}
