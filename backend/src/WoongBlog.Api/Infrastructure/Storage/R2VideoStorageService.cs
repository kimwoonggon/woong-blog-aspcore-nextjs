using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

namespace WoongBlog.Api.Infrastructure.Storage;

public sealed class R2VideoStorageService(
    IOptions<CloudflareR2Options> options) : IVideoObjectStorage
{
    private readonly CloudflareR2Options _options = options.Value;
    private AmazonS3Client? _client;

    public string StorageType => WorkVideoSourceTypes.R2;

    public string? BuildPlaybackUrl(string storageKey)
    {
        if (!_options.IsConfigured())
        {
            return null;
        }

        return $"{_options.PublicUrl.TrimEnd('/')}/{storageKey}";
    }

    public Task<VideoUploadTargetResult> CreateUploadTargetAsync(
        Guid workId,
        Guid uploadSessionId,
        string storageKey,
        string contentType,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(30),
            ContentType = contentType
        };

        var signingClient = string.IsNullOrWhiteSpace(_options.BrowserEndpoint)
            ? GetClient()
            : BuildClient(_options.AccessKeyId, _options.SecretAccessKey, _options.BrowserEndpoint);
        var url = signingClient.GetPreSignedURL(request);
        return Task.FromResult(new VideoUploadTargetResult(uploadSessionId, "PUT", url, storageKey));
    }

    public async Task SaveDirectUploadAsync(string storageKey, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        await GetClient().PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            InputStream = stream,
            ContentType = contentType,
            UseChunkEncoding = false
        }, cancellationToken);
    }

    public async Task<VideoStoredObject?> GetObjectAsync(string storageKey, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        try
        {
            var response = await GetClient().GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey
            }, cancellationToken);

            return new VideoStoredObject(response.Headers.ContentType, response.Headers.ContentLength);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<byte[]> ReadPrefixAsync(string storageKey, int length, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var response = await GetClient().GetObjectAsync(new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            ByteRange = new ByteRange(0, Math.Max(0, length - 1))
        }, cancellationToken);

        await using var stream = response.ResponseStream;
        var buffer = new byte[length];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, length), cancellationToken);
        return buffer[..bytesRead];
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        if (storageKey.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            await DeleteHlsObjectsAsync(storageKey, cancellationToken);
            return;
        }

        await GetClient().DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey
        }, cancellationToken);
    }

    private async Task DeleteHlsObjectsAsync(string manifestStorageKey, CancellationToken cancellationToken)
    {
        var separatorIndex = manifestStorageKey.LastIndexOf('/');
        if (separatorIndex < 0)
        {
            await GetClient().DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = manifestStorageKey
            }, cancellationToken);
            return;
        }

        var prefix = manifestStorageKey[..(separatorIndex + 1)];
        var request = new ListObjectsV2Request
        {
            BucketName = _options.BucketName,
            Prefix = prefix
        };

        do
        {
            var listedObjects = await GetClient().ListObjectsV2Async(request, cancellationToken);
            foreach (var storedObject in listedObjects.S3Objects)
            {
                await GetClient().DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = storedObject.Key
                }, cancellationToken);
            }

            request.ContinuationToken = listedObjects.NextContinuationToken;
        }
        while (!string.IsNullOrWhiteSpace(request.ContinuationToken));
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured())
        {
            throw new InvalidOperationException("Cloudflare R2 is not configured.");
        }
    }

    private AmazonS3Client GetClient()
    {
        EnsureConfigured();
        return _client ??= BuildClient(_options.AccessKeyId, _options.SecretAccessKey, _options.Endpoint);
    }

    private static AmazonS3Client BuildClient(string accessKeyId, string secretAccessKey, string endpoint)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        return new AmazonS3Client(accessKeyId, secretAccessKey, config);
    }
}
