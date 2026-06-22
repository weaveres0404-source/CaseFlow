using Google.Cloud.Storage.V1;

namespace CaseFlow.Server.Services;

public class GcsStorageService
{
    private readonly StorageClient? _storageClient;

    private const string BucketName = "caseflow-test-files";

    public GcsStorageService()
    {
        try
        {
            _storageClient = StorageClient.Create();
        }
        catch
        {
            // 本地開發環境無 GCP 憑證時允許啟動，上傳功能不可用
            _storageClient = null;
        }
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string objectName)
    {
        if (_storageClient == null)
            throw new InvalidOperationException("GCS storage client is not available (missing credentials).");

        using var stream = file.OpenReadStream();

        await _storageClient.UploadObjectAsync(
            BucketName,
            objectName,
            file.ContentType,
            stream);

        return $"https://storage.googleapis.com/{BucketName}/{objectName}";
    }

    public async Task<Stream> DownloadAsync(
        string objectName)
    {
        if (_storageClient == null)
            throw new InvalidOperationException("GCS storage client is not available (missing credentials).");

        var stream = new MemoryStream();

        await _storageClient.DownloadObjectAsync(
            BucketName,
            objectName,
            stream);

        stream.Position = 0;

        return stream;
    }

    public async Task DeleteAsync(
        string objectName)
    {
        if (_storageClient == null)
            throw new InvalidOperationException("GCS storage client is not available (missing credentials).");

        await _storageClient.DeleteObjectAsync(
            BucketName,
            objectName);
    }
}