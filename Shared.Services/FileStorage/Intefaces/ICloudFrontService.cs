namespace Shared.Services.FileStorage.Intefaces
{
    public interface ICloudFrontService
    {
        /// <summary>
        /// Invalidates CloudFront cache for the given paths.
        /// Paths should start with '/' (e.g., "/images/profile-pictures/user123.jpg").
        /// No-op if CloudFront distribution ID is not configured.
        /// </summary>
        Task InvalidateAsync(params string[] paths);
    }
}
