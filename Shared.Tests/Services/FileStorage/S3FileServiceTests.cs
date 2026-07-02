using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shared.Options;
using Shared.Services.FileStorage;

namespace Shared.Tests.Services.FileStorage;

[TestFixture]
[Category("Unit")]
public class S3FileServiceTests
{
    private IAmazonS3 _s3Client = null!;
    private S3FileService _sut = null!;
    private CopyObjectRequest? _capturedCopy;

    [SetUp]
    public void SetUp()
    {
        _s3Client = Substitute.For<IAmazonS3>();
        _capturedCopy = null;
        _s3Client.CopyObjectAsync(
                Arg.Do<CopyObjectRequest>(r => _capturedCopy = r),
                Arg.Any<CancellationToken>())
            .Returns(new CopyObjectResponse());

        _sut = new S3FileService(_s3Client, new OptionsWrapper<S3Settings>(new S3Settings
        {
            Bucket = "test-bucket",
            PublicBaseUrl = "https://cdn.test",
            PresignedUrlExpiryMinutes = 15
        }));
    }

    [TearDown]
    public void TearDown() => _s3Client.Dispose();

    [Test]
    public async Task Move_WithContentMetadata_ReplacesMetadataWithProvidedValues()
    {
        await _sut.MoveObjectAsync("temp/a.pdf", "posts/a.pdf", "test-bucket",
            cacheControl: CacheControlValues.Immutable,
            contentType: "application/pdf",
            contentDisposition: ContentDispositionValue.Inline("report.pdf"));

        Assert.That(_capturedCopy, Is.Not.Null);
        Assert.That(_capturedCopy!.MetadataDirective, Is.EqualTo(S3MetadataDirective.REPLACE));
        Assert.That(_capturedCopy.Headers.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(_capturedCopy.Headers.CacheControl, Is.EqualTo(CacheControlValues.Immutable));
        Assert.That(_capturedCopy.Headers.ContentDisposition,
            Is.EqualTo("inline; filename=\"report.pdf\""));
        await _s3Client.DidNotReceive().GetObjectMetadataAsync(
            Arg.Any<GetObjectMetadataRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Move_WithCacheControlOnly_PreservesSourceContentType()
    {
        var sourceMetadata = new GetObjectMetadataResponse();
        sourceMetadata.Headers.ContentType = "application/pdf";
        _s3Client.GetObjectMetadataAsync(
                Arg.Is<GetObjectMetadataRequest>(r => r.BucketName == "test-bucket" && r.Key == "temp/a.pdf"),
                Arg.Any<CancellationToken>())
            .Returns(sourceMetadata);

        await _sut.MoveObjectAsync("temp/a.pdf", "posts/a.pdf", "test-bucket",
            cacheControl: CacheControlValues.NoStore);

        Assert.That(_capturedCopy, Is.Not.Null);
        Assert.That(_capturedCopy!.MetadataDirective, Is.EqualTo(S3MetadataDirective.REPLACE));
        Assert.That(_capturedCopy.Headers.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(_capturedCopy.Headers.CacheControl, Is.EqualTo(CacheControlValues.NoStore));
    }

    [Test]
    public async Task Move_WithoutMetadataChanges_CopiesMetadataAsIs()
    {
        await _sut.MoveObjectAsync("temp/a.pdf", "posts/a.pdf", "test-bucket");

        Assert.That(_capturedCopy, Is.Not.Null);
        Assert.That(_capturedCopy!.MetadataDirective, Is.Not.EqualTo(S3MetadataDirective.REPLACE));
        await _s3Client.DidNotReceive().GetObjectMetadataAsync(
            Arg.Any<GetObjectMetadataRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Move_DeletesSourceObjectAfterCopy()
    {
        await _sut.MoveObjectAsync("temp/a.pdf", "posts/a.pdf", "test-bucket");

        await _s3Client.Received(1).DeleteObjectAsync(
            Arg.Is<DeleteObjectRequest>(r => r.BucketName == "test-bucket" && r.Key == "temp/a.pdf"),
            Arg.Any<CancellationToken>());
    }
}
