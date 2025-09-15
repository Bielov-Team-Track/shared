namespace Shared.Options
{
    public class S3Settings
    {
        public required string Bucket { get; set; }
        public int PresignedUrlExpiryMinutes { get; set; }
    }
}
