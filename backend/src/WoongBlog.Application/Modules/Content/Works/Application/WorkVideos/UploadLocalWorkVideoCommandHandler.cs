using MediatR;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

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
