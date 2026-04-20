using MediatR;
using WoongBlog.Api.Domain.Entities;

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
