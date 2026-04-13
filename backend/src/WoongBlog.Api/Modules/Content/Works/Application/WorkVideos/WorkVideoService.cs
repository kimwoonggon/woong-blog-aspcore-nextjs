using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Infrastructure.Storage;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class WorkVideoService(
    WoongBlogDbContext dbContext,
    IEnumerable<IVideoObjectStorage> storages,
    IWorkVideoPlaybackUrlBuilder playbackUrlBuilder,
    IHostEnvironment environment,
    IOptions<CloudflareR2Options> r2Options) : IWorkVideoService
{
    private const int MaxVideosPerWork = 10;
    private const long MaxVideoBytes = 200L * 1024L * 1024L;
    private static readonly string[] AllowedMimeTypes = ["video/mp4"];
    private static readonly string[] AllowedExtensions = [".mp4"];

    private readonly WoongBlogDbContext _dbContext = dbContext;
    private readonly IWorkVideoPlaybackUrlBuilder _playbackUrlBuilder = playbackUrlBuilder;
    private readonly IHostEnvironment _environment = environment;
    private readonly CloudflareR2Options _r2Options = r2Options.Value;
    private readonly IReadOnlyDictionary<string, IVideoObjectStorage> _storages = storages
        .ToDictionary(storage => storage.StorageType, StringComparer.OrdinalIgnoreCase);

    public async Task<WorkVideoServiceResult<VideoUploadTargetResult>> IssueUploadAsync(
        Guid workId,
        string fileName,
        string contentType,
        long size,
        int expectedVideosVersion,
        CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.SingleOrDefaultAsync(x => x.Id == workId, cancellationToken);
        if (work is null)
        {
            return WorkVideoServiceResult<VideoUploadTargetResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != expectedVideosVersion)
        {
            return WorkVideoServiceResult<VideoUploadTargetResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var validationError = ValidateVideoFile(fileName, contentType, size);
        if (validationError is not null)
        {
            return WorkVideoServiceResult<VideoUploadTargetResult>.BadRequest(validationError);
        }

        if (await CountVideosAsync(workId, cancellationToken) >= MaxVideosPerWork)
        {
            return WorkVideoServiceResult<VideoUploadTargetResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var storageType = ResolveStorageType();
        if (!_storages.TryGetValue(storageType, out var storage))
        {
            return WorkVideoServiceResult<VideoUploadTargetResult>.BadRequest("No video storage backend is available.");
        }

        var sessionId = Guid.NewGuid();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var storageKey = $"videos/{workId:N}/{sessionId:N}{extension}";
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var session = new WorkVideoUploadSession
        {
            Id = sessionId,
            WorkId = workId,
            StorageType = storageType,
            StorageKey = storageKey,
            OriginalFileName = SanitizeOriginalFileName(fileName),
            ExpectedMimeType = contentType,
            ExpectedSize = size,
            ExpiresAt = expiresAt,
            Status = WorkVideoUploadSessionStatuses.Issued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.WorkVideoUploadSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var target = await storage.CreateUploadTargetAsync(workId, sessionId, storageKey, contentType, cancellationToken);
        return WorkVideoServiceResult<VideoUploadTargetResult>.Ok(target);
    }

    public async Task<WorkVideoServiceResult<object>> UploadLocalAsync(
        Guid workId,
        Guid uploadSessionId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return WorkVideoServiceResult<object>.BadRequest("No file uploaded.");
        }

        var session = await _dbContext.WorkVideoUploadSessions.SingleOrDefaultAsync(
            x => x.Id == uploadSessionId && x.WorkId == workId,
            cancellationToken);

        if (session is null)
        {
            return WorkVideoServiceResult<object>.NotFound("Upload session not found.");
        }

        if (!string.Equals(session.StorageType, WorkVideoSourceTypes.Local, StringComparison.OrdinalIgnoreCase))
        {
            return WorkVideoServiceResult<object>.Unsupported("Direct upload is only available for local video storage.");
        }

        var validationError = ValidateVideoFile(file.FileName, file.ContentType, file.Length);
        if (validationError is not null)
        {
            return WorkVideoServiceResult<object>.BadRequest(validationError);
        }

        if (!_storages.TryGetValue(session.StorageType, out var storage))
        {
            return WorkVideoServiceResult<object>.BadRequest("Local video storage is not available.");
        }

        await using var stream = file.OpenReadStream();
        await storage.SaveDirectUploadAsync(session.StorageKey, stream, file.ContentType, cancellationToken);
        session.Status = WorkVideoUploadSessionStatuses.Uploaded;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return WorkVideoServiceResult<object>.Ok(new { success = true });
    }

    public async Task<WorkVideoServiceResult<WorkVideosMutationResult>> ConfirmUploadAsync(
        Guid workId,
        Guid uploadSessionId,
        int expectedVideosVersion,
        CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.SingleOrDefaultAsync(x => x.Id == workId, cancellationToken);
        if (work is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != expectedVideosVersion)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var session = await _dbContext.WorkVideoUploadSessions.SingleOrDefaultAsync(
            x => x.Id == uploadSessionId && x.WorkId == workId,
            cancellationToken);

        if (session is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.NotFound("Upload session not found.");
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            session.Status = WorkVideoUploadSessionStatuses.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Upload session expired.");
        }

        if (!_storages.TryGetValue(session.StorageType, out var storage))
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Video storage backend is not available.");
        }

        var storedObject = await storage.GetObjectAsync(session.StorageKey, cancellationToken);
        if (storedObject is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Uploaded object was not found.");
        }

        if (!string.Equals(session.ExpectedMimeType, storedObject.ContentType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(session.StorageType, WorkVideoSourceTypes.Local, StringComparison.OrdinalIgnoreCase))
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Uploaded object content type did not match.");
        }

        if (storedObject.Size != session.ExpectedSize)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Uploaded object size did not match.");
        }

        var prefix = await storage.ReadPrefixAsync(session.StorageKey, 64, cancellationToken);
        if (!LooksLikeMp4(prefix))
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Only valid MP4 files are supported.");
        }

        if (await CountVideosAsync(workId, cancellationToken) >= MaxVideosPerWork)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var sortOrder = await GetNextSortOrderAsync(workId, cancellationToken);
        var video = new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = session.StorageType,
            SourceKey = session.StorageKey,
            OriginalFileName = session.OriginalFileName,
            MimeType = storedObject.ContentType ?? session.ExpectedMimeType,
            FileSize = storedObject.Size,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.WorkVideos.Add(video);
        session.Status = WorkVideoUploadSessionStatuses.Confirmed;
        work.VideosVersion += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return WorkVideoServiceResult<WorkVideosMutationResult>.Ok(await BuildMutationResultAsync(workId, cancellationToken));
    }

    public async Task<WorkVideoServiceResult<WorkVideosMutationResult>> AddYouTubeAsync(
        Guid workId,
        string youtubeUrlOrId,
        int expectedVideosVersion,
        CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.SingleOrDefaultAsync(x => x.Id == workId, cancellationToken);
        if (work is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != expectedVideosVersion)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        if (await CountVideosAsync(workId, cancellationToken) >= MaxVideosPerWork)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var videoId = NormalizeYouTubeVideoId(youtubeUrlOrId);
        if (videoId is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Provide a valid YouTube video URL or ID.");
        }

        _dbContext.WorkVideos.Add(new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = videoId,
            SortOrder = await GetNextSortOrderAsync(workId, cancellationToken),
            CreatedAt = DateTimeOffset.UtcNow
        });

        work.VideosVersion += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return WorkVideoServiceResult<WorkVideosMutationResult>.Ok(await BuildMutationResultAsync(workId, cancellationToken));
    }

    public async Task<WorkVideoServiceResult<WorkVideosMutationResult>> ReorderAsync(
        Guid workId,
        IReadOnlyList<Guid> orderedVideoIds,
        int expectedVideosVersion,
        CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.SingleOrDefaultAsync(x => x.Id == workId, cancellationToken);
        if (work is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != expectedVideosVersion)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var videos = await _dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (videos.Count != orderedVideoIds.Count
            || videos.Any(video => !orderedVideoIds.Contains(video.Id)))
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.BadRequest("Reorder payload must include every video exactly once.");
        }

        // Avoid unique index collisions on (WorkId, SortOrder) while swapping rows.
        for (var index = 0; index < videos.Count; index += 1)
        {
            videos[index].SortOrder = orderedVideoIds.Count + index;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < orderedVideoIds.Count; index += 1)
        {
            videos.Single(video => video.Id == orderedVideoIds[index]).SortOrder = index;
        }

        work.VideosVersion += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return WorkVideoServiceResult<WorkVideosMutationResult>.Ok(await BuildMutationResultAsync(workId, cancellationToken));
    }

    public async Task<WorkVideoServiceResult<WorkVideosMutationResult>> DeleteAsync(
        Guid workId,
        Guid videoId,
        int expectedVideosVersion,
        CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.SingleOrDefaultAsync(x => x.Id == workId, cancellationToken);
        if (work is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != expectedVideosVersion)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var video = await _dbContext.WorkVideos.SingleOrDefaultAsync(x => x.Id == videoId && x.WorkId == workId, cancellationToken);
        if (video is null)
        {
            return WorkVideoServiceResult<WorkVideosMutationResult>.NotFound("Video not found.");
        }

        await EnqueueCleanupAsync(workId, video.Id, video.SourceType, video.SourceKey, cancellationToken);
        _dbContext.WorkVideos.Remove(video);

        var remainingVideos = await _dbContext.WorkVideos
            .Where(x => x.WorkId == workId && x.Id != videoId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < remainingVideos.Count; index += 1)
        {
            remainingVideos[index].SortOrder = index;
        }

        work.VideosVersion += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return WorkVideoServiceResult<WorkVideosMutationResult>.Ok(await BuildMutationResultAsync(workId, cancellationToken));
    }

    public async Task EnqueueCleanupForWorkAsync(Guid workId, CancellationToken cancellationToken)
    {
        var videos = await _dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .ToListAsync(cancellationToken);

        foreach (var video in videos)
        {
            await EnqueueCleanupAsync(workId, video.Id, video.SourceType, video.SourceKey, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ProcessCleanupJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.VideoStorageCleanupJobs
            .Where(x => x.Status == VideoStorageCleanupJobStatuses.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            if (!_storages.TryGetValue(job.StorageType, out var storage))
            {
                job.Status = VideoStorageCleanupJobStatuses.Failed;
                job.LastError = "Storage backend not available.";
                job.UpdatedAt = DateTimeOffset.UtcNow;
                continue;
            }

            try
            {
                await storage.DeleteAsync(job.StorageKey, cancellationToken);
                job.Status = VideoStorageCleanupJobStatuses.Succeeded;
                job.LastError = null;
            }
            catch (Exception exception)
            {
                job.AttemptCount += 1;
                job.LastError = exception.Message;
                job.Status = job.AttemptCount >= 5
                    ? VideoStorageCleanupJobStatuses.Failed
                    : VideoStorageCleanupJobStatuses.Pending;
            }

            job.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if (jobs.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return jobs.Count;
    }

    public async Task<int> ExpireUploadSessionsAsync(CancellationToken cancellationToken)
    {
        var sessions = await _dbContext.WorkVideoUploadSessions
            .Where(x => x.ExpiresAt <= DateTimeOffset.UtcNow && x.Status != WorkVideoUploadSessionStatuses.Confirmed && x.Status != WorkVideoUploadSessionStatuses.Expired)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            await EnqueueCleanupAsync(session.WorkId, null, session.StorageType, session.StorageKey, cancellationToken);
            session.Status = WorkVideoUploadSessionStatuses.Expired;
        }

        if (sessions.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return sessions.Count;
    }

    private async Task<WorkVideosMutationResult> BuildMutationResultAsync(Guid workId, CancellationToken cancellationToken)
    {
        var work = await _dbContext.Works.AsNoTracking().SingleAsync(x => x.Id == workId, cancellationToken);
        var videos = await _dbContext.WorkVideos
            .AsNoTracking()
            .Where(x => x.WorkId == workId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return new WorkVideosMutationResult(
            work.VideosVersion,
            videos.Select(video => new WorkVideoDto(
                video.Id,
                video.SourceType,
                video.SourceKey,
                _playbackUrlBuilder.BuildPlaybackUrl(video.SourceType, video.SourceKey),
                video.OriginalFileName,
                video.MimeType,
                video.FileSize,
                video.SortOrder,
                video.CreatedAt
            )).ToList());
    }

    private async Task EnqueueCleanupAsync(
        Guid? workId,
        Guid? workVideoId,
        string sourceType,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        if (string.Equals(sourceType, WorkVideoSourceTypes.YouTube, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var exists = await _dbContext.VideoStorageCleanupJobs.AnyAsync(
            x => x.StorageType == sourceType && x.StorageKey == sourceKey && x.Status == VideoStorageCleanupJobStatuses.Pending,
            cancellationToken);

        if (exists)
        {
            return;
        }

        _dbContext.VideoStorageCleanupJobs.Add(new VideoStorageCleanupJob
        {
            Id = Guid.NewGuid(),
            WorkId = workId,
            WorkVideoId = workVideoId,
            StorageType = sourceType,
            StorageKey = sourceKey,
            AttemptCount = 0,
            Status = VideoStorageCleanupJobStatuses.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }

    private static bool LooksLikeMp4(byte[] prefix)
    {
        if (prefix.Length < 12)
        {
            return false;
        }

        for (var index = 4; index <= prefix.Length - 4; index += 1)
        {
            if (prefix[index] == (byte)'f'
                && prefix[index + 1] == (byte)'t'
                && prefix[index + 2] == (byte)'y'
                && prefix[index + 3] == (byte)'p')
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeYouTubeVideoId(string rawValue)
    {
        var trimmed = rawValue.Trim();
        if (Regex.IsMatch(trimmed, "^[A-Za-z0-9_-]{11}$"))
        {
            return trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host is "youtu.be")
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 && Regex.IsMatch(segments[0], "^[A-Za-z0-9_-]{11}$") ? segments[0] : null;
        }

        if (host is not "www.youtube.com" and not "youtube.com" and not "m.youtube.com")
        {
            return null;
        }

        if (uri.AbsolutePath.StartsWith("/watch", StringComparison.OrdinalIgnoreCase))
        {
            var parsedQuery = QueryHelpers.ParseQuery(uri.Query);
            var videoId = parsedQuery.TryGetValue("v", out var queryValue) ? queryValue.ToString() : null;
            return !string.IsNullOrWhiteSpace(videoId) && Regex.IsMatch(videoId, "^[A-Za-z0-9_-]{11}$") ? videoId : null;
        }

        var pathSegments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length == 2 && (pathSegments[0] == "embed" || pathSegments[0] == "shorts"))
        {
            return Regex.IsMatch(pathSegments[1], "^[A-Za-z0-9_-]{11}$") ? pathSegments[1] : null;
        }

        return null;
    }

    private string ResolveStorageType()
    {
        if (_environment.IsDevelopment() || _environment.IsEnvironment("Testing"))
        {
            if (!_r2Options.ForceEnabledInDevelopment)
            {
                return WorkVideoSourceTypes.Local;
            }
        }

        return _storages.ContainsKey(WorkVideoSourceTypes.R2)
            && _storages[WorkVideoSourceTypes.R2].BuildPlaybackUrl("health-check") is not null
            ? WorkVideoSourceTypes.R2
            : WorkVideoSourceTypes.Local;
    }

    private static string SanitizeOriginalFileName(string fileName)
    {
        var sanitized = Path.GetFileName(fileName).Trim();
        if (sanitized.Length <= 120)
        {
            return sanitized;
        }

        return sanitized[..120];
    }

    private async Task<int> CountVideosAsync(Guid workId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkVideos.CountAsync(x => x.WorkId == workId, cancellationToken);
    }

    private async Task<int> GetNextSortOrderAsync(Guid workId, CancellationToken cancellationToken)
    {
        var maxSortOrder = await _dbContext.WorkVideos
            .Where(x => x.WorkId == workId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);

        return (maxSortOrder ?? -1) + 1;
    }

    private static string? ValidateVideoFile(string fileName, string contentType, long size)
    {
        if (size <= 0)
        {
            return "Video file size must be greater than zero.";
        }

        if (size > MaxVideoBytes)
        {
            return "Video file size must be 200MB or smaller.";
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return "Only .mp4 uploads are supported.";
        }

        if (!AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return "Only video/mp4 uploads are supported.";
        }

        return null;
    }
}
