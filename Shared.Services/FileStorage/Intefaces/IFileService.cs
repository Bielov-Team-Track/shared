namespace Shared.Services.FileStorage.Intefaces
{
    public interface IFileService
    {
        Task<string> GetPresignedUploadLink(string fileName, string folder, string contentType);
    }
}
