using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Storage;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class IssueWorkVideoUploadCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoStorageSelector storageSelector)
    : IRequestHandler<IssueWorkVideoUploadCommand, WorkVideoResult<VideoUploadTargetResult>>
{
    public async Task<WorkVideoResult<VideoUploadTargetResult>> Handle(
        IssueWorkVideoUploadCommand request,
        CancellationToken cancellationToken)
    {
        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<VideoUploadTargetResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<VideoUploadTargetResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var validationError = WorkVideoPolicy.ValidateVideoFile(request.FileName, request.ContentType, request.Size);
        if (validationError is not null)
        {
            return WorkVideoResult<VideoUploadTargetResult>.BadRequest(validationError);
        }

        if (await commandStore.CountVideosAsync(request.WorkId, cancellationToken) >= WorkVideoPolicy.MaxVideosPerWork)
        {
            return WorkVideoResult<VideoUploadTargetResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var storageType = storageSelector.ResolveStorageType();
        if (!storageSelector.TryGetStorage(storageType, out var storage))
        {
            return WorkVideoResult<VideoUploadTargetResult>.BadRequest("No video storage backend is available.");
        }

        var sessionId = Guid.NewGuid();
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var storageKey = $"videos/{request.WorkId:N}/{sessionId:N}{extension}";
        var session = new WorkVideoUploadSession
        {
            Id = sessionId,
            WorkId = request.WorkId,
            StorageType = storageType,
            StorageKey = storageKey,
            OriginalFileName = WorkVideoPolicy.SanitizeOriginalFileName(request.FileName),
            ExpectedMimeType = request.ContentType,
            ExpectedSize = request.Size,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Status = WorkVideoUploadSessionStatuses.Issued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        commandStore.AddUploadSession(session);
        await commandStore.SaveChangesAsync(cancellationToken);

        var target = await storage.CreateUploadTargetAsync(request.WorkId, sessionId, storageKey, request.ContentType, cancellationToken);
        return WorkVideoResult<VideoUploadTargetResult>.Ok(target);
    }
}

public sealed class UploadLocalWorkVideoCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoStorageSelector storageSelector)
    : IRequestHandler<UploadLocalWorkVideoCommand, WorkVideoResult<object>>
{
    public async Task<WorkVideoResult<object>> Handle(
        UploadLocalWorkVideoCommand request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return WorkVideoResult<object>.BadRequest("No file uploaded.");
        }

        var session = await commandStore.GetUploadSessionForUpdateAsync(request.WorkId, request.UploadSessionId, cancellationToken);
        if (session is null)
        {
            return WorkVideoResult<object>.NotFound("Upload session not found.");
        }

        if (!string.Equals(session.StorageType, WorkVideoSourceTypes.Local, StringComparison.OrdinalIgnoreCase))
        {
            return WorkVideoResult<object>.Unsupported("Direct upload is only available for local video storage.");
        }

        var validationError = WorkVideoPolicy.ValidateVideoFile(request.File.FileName, request.File.ContentType, request.File.Length);
        if (validationError is not null)
        {
            return WorkVideoResult<object>.BadRequest(validationError);
        }

        if (!storageSelector.TryGetStorage(session.StorageType, out var storage))
        {
            return WorkVideoResult<object>.BadRequest("Local video storage is not available.");
        }

        await using var stream = request.File.OpenReadStream();
        await storage.SaveDirectUploadAsync(session.StorageKey, stream, request.File.ContentType, cancellationToken);
        session.Status = WorkVideoUploadSessionStatuses.Uploaded;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<object>.Ok(new { success = true });
    }
}

public sealed class ConfirmWorkVideoUploadCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore,
    IWorkVideoStorageSelector storageSelector,
    IWorkVideoFileInspector fileInspector)
    : IRequestHandler<ConfirmWorkVideoUploadCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        ConfirmWorkVideoUploadCommand request,
        CancellationToken cancellationToken)
    {
        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var session = await commandStore.GetUploadSessionForUpdateAsync(request.WorkId, request.UploadSessionId, cancellationToken);
        if (session is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Upload session not found.");
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            session.Status = WorkVideoUploadSessionStatuses.Expired;
            await commandStore.SaveChangesAsync(cancellationToken);
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Upload session expired.");
        }

        if (!storageSelector.TryGetStorage(session.StorageType, out var storage))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Video storage backend is not available.");
        }

        var storedObject = await storage.GetObjectAsync(session.StorageKey, cancellationToken);
        if (storedObject is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Uploaded object was not found.");
        }

        if (!string.Equals(session.ExpectedMimeType, storedObject.ContentType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(session.StorageType, WorkVideoSourceTypes.Local, StringComparison.OrdinalIgnoreCase))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Uploaded object content type did not match.");
        }

        if (storedObject.Size != session.ExpectedSize)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Uploaded object size did not match.");
        }

        if (!await fileInspector.LooksLikeMp4Async(session.StorageKey, storage, cancellationToken))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Only valid MP4 files are supported.");
        }

        if (await commandStore.CountVideosAsync(request.WorkId, cancellationToken) >= WorkVideoPolicy.MaxVideosPerWork)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        commandStore.AddWorkVideo(new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = request.WorkId,
            SourceType = session.StorageType,
            SourceKey = session.StorageKey,
            OriginalFileName = session.OriginalFileName,
            MimeType = storedObject.ContentType ?? session.ExpectedMimeType,
            FileSize = storedObject.Size,
            SortOrder = await commandStore.GetNextSortOrderAsync(request.WorkId, cancellationToken),
            CreatedAt = DateTimeOffset.UtcNow
        });

        session.Status = WorkVideoUploadSessionStatuses.Confirmed;
        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }
}

public sealed class AddYouTubeWorkVideoCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore)
    : IRequestHandler<AddYouTubeWorkVideoCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        AddYouTubeWorkVideoCommand request,
        CancellationToken cancellationToken)
    {
        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        if (await commandStore.CountVideosAsync(request.WorkId, cancellationToken) >= WorkVideoPolicy.MaxVideosPerWork)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var videoId = WorkVideoPolicy.NormalizeYouTubeVideoId(request.YoutubeUrlOrId);
        if (videoId is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Provide a valid YouTube video URL or ID.");
        }

        commandStore.AddWorkVideo(new WorkVideo
        {
            Id = Guid.NewGuid(),
            WorkId = request.WorkId,
            SourceType = WorkVideoSourceTypes.YouTube,
            SourceKey = videoId,
            SortOrder = await commandStore.GetNextSortOrderAsync(request.WorkId, cancellationToken),
            CreatedAt = DateTimeOffset.UtcNow
        });

        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }
}

public sealed class ReorderWorkVideosCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore)
    : IRequestHandler<ReorderWorkVideosCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        ReorderWorkVideosCommand request,
        CancellationToken cancellationToken)
    {
        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var videos = await commandStore.GetVideosForWorkAsync(request.WorkId, cancellationToken);
        if (videos.Count != request.OrderedVideoIds.Count
            || videos.Any(video => !request.OrderedVideoIds.Contains(video.Id)))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Reorder payload must include every video exactly once.");
        }

        for (var index = 0; index < videos.Count; index += 1)
        {
            videos[index].SortOrder = request.OrderedVideoIds.Count + index;
        }

        await commandStore.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < request.OrderedVideoIds.Count; index += 1)
        {
            videos.Single(video => video.Id == request.OrderedVideoIds[index]).SortOrder = index;
        }

        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }
}

public sealed class DeleteWorkVideoCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore)
    : IRequestHandler<DeleteWorkVideoCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        DeleteWorkVideoCommand request,
        CancellationToken cancellationToken)
    {
        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var video = await commandStore.GetVideoForUpdateAsync(request.WorkId, request.VideoId, cancellationToken);
        if (video is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Video not found.");
        }

        await commandStore.EnqueueCleanupAsync(
            request.WorkId,
            video.Id,
            video.SourceType,
            video.SourceKey,
            DateTimeOffset.UtcNow,
            cancellationToken);
        commandStore.RemoveWorkVideo(video);

        var remainingVideos = (await commandStore.GetVideosForWorkAsync(request.WorkId, cancellationToken))
            .Where(x => x.Id != request.VideoId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        for (var index = 0; index < remainingVideos.Count; index += 1)
        {
            remainingVideos[index].SortOrder = index;
        }

        work.VideosVersion += 1;
        await commandStore.SaveChangesAsync(cancellationToken);
        return WorkVideoResult<WorkVideosMutationResult>.Ok(
            await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
    }
}

internal static class WorkVideoPolicy
{
    public const int MaxVideosPerWork = 10;
    public const long MaxVideoBytes = 200L * 1024L * 1024L;
    public const string HlsManifestFileName = "master.m3u8";
    public const string HlsManifestContentType = "application/vnd.apple.mpegurl";
    public const string HlsSegmentContentType = "video/mp2t";

    private static readonly string[] AllowedMimeTypes = ["video/mp4"];
    private static readonly string[] AllowedExtensions = [".mp4"];

    public static string? ValidateVideoFile(string fileName, string contentType, long size)
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

    public static bool LooksLikeMp4(byte[] prefix)
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

    public static string? NormalizeYouTubeVideoId(string rawValue)
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

    public static string SanitizeOriginalFileName(string fileName)
    {
        var sanitized = Path.GetFileName(fileName).Trim();
        return sanitized.Length <= 120 ? sanitized : sanitized[..120];
    }
}
