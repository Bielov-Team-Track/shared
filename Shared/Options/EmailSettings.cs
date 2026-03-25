namespace Shared.Options
{
    public class EmailSettings
    {
        public required string FromEmail { get; set; }
        public bool SmtpTransportEnabled { get; set; }
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 1025;
        public bool SmtpUseTls { get; set; }
    }
}
