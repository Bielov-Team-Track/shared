namespace Shared.Services.FileStorage.Intefaces
{
    public interface IFileService
    {
        Task<string> GetPresignedUploadLink(string fileName, string folder, string contentType,
            string? cacheControl = null);

        /// <summary>
        /// Moves an object from one key to another within the same bucket.
        /// Optionally sets Cache-Control, Content-Type and Content-Disposition metadata
        /// on the destination object. When any metadata is set, Content-Type is preserved
        /// from the source object unless an explicit <paramref name="contentType"/> is given.
        /// </summary>
        Task MoveObjectAsync(string sourceKey, string destinationKey, string bucket,
            string? cacheControl = null, string? contentType = null, string? contentDisposition = null);

        /// <summary>
        /// Deletes an object from the bucket.
        /// </summary>
        Task DeleteObjectAsync(string key, string bucket);

        /// <summary>
        /// Checks if an object exists in the bucket.
        /// </summary>
        Task<bool> ObjectExistsAsync(string key, string bucket);

        /// <summary>
        /// Gets the public URL for an object. Uses configured base URL (S3 or CDN).
        /// </summary>
        string GetPublicUrl(string key);

        /// <summary>
        /// Reads the first N bytes of an object via a range GET.
        /// </summary>
        Task<byte[]> GetObjectHeadBytesAsync(string key, string bucket, int byteCount);
    }
}
