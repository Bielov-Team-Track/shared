using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Services.FileStorage.Intefaces;

namespace Shared.Services.FileStorage
{
    public class S3FileService : IFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Settings _s3Settings;

        public S3FileService(IAmazonS3 s3Client, IOptions<S3Settings> s3Settings)
        {
            _s3Client = s3Client;
            _s3Settings = s3Settings.Value;
        }

        public async Task<string> GetPresignedUploadLink(string fileName, string folder, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = folder,
                Key = fileName,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignedUrlExpiryMinutes),
                ContentType = contentType
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }
    }
}
