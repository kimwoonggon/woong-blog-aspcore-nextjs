using MediatR;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class StartWorkVideoHlsJobCommandHandler(
    IWorkVideoCommandStore commandStore,
    IWorkVideoQueryStore queryStore,
    IWorkVideoStorageSelector storageSelector,
    IVideoTranscoder videoTranscoder,
    IWorkVideoFileInspector fileInspector,
    IWorkVideoHlsWorkspace hlsWorkspace,
    IWorkVideoHlsOutputPublisher hlsOutputPublisher)
    : IRequestHandler<StartWorkVideoHlsJobCommand, WorkVideoResult<WorkVideosMutationResult>>
{
    public async Task<WorkVideoResult<WorkVideosMutationResult>> Handle(
        StartWorkVideoHlsJobCommand request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("No file uploaded.");
        }

        var work = await commandStore.GetWorkForUpdateAsync(request.WorkId, cancellationToken);
        if (work is null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.NotFound("Work not found.");
        }

        if (work.VideosVersion != request.ExpectedVideosVersion)
        {
            return WorkVideoResult<WorkVideosMutationResult>.Conflict("Videos changed. Refresh and retry.");
        }

        var validationError = WorkVideoPolicy.ValidateVideoFile(request.File.FileName, request.File.ContentType, request.File.Length);
        if (validationError is not null)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest(validationError);
        }

        if (await commandStore.CountVideosAsync(request.WorkId, cancellationToken) >= WorkVideoPolicy.MaxVideosPerWork)
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Each work supports up to 10 videos.");
        }

        var storageType = storageSelector.ResolveStorageType();
        if (!storageSelector.TryGetStorage(storageType, out var storage))
        {
            return WorkVideoResult<WorkVideosMutationResult>.BadRequest("No video storage backend is available.");
        }

        var plan = WorkVideoHlsJobPlan.Create(
            request.WorkId,
            storageType,
            request.File.FileName,
            request.File.Length);

        await using (var workspace = await hlsWorkspace.CreateAsync(request.File, plan.VideoId, cancellationToken))
        {
            if (!await fileInspector.LooksLikeMp4Async(workspace.SourcePath, cancellationToken))
            {
                return WorkVideoResult<WorkVideosMutationResult>.BadRequest("Only valid MP4 files are supported.");
            }

            var ffmpegError = await videoTranscoder.SegmentHlsAsync(
                workspace.SourcePath,
                workspace.HlsDirectory,
                WorkVideoPolicy.HlsManifestFileName,
                cancellationToken);
            if (ffmpegError is not null)
            {
                return WorkVideoResult<WorkVideosMutationResult>.BadRequest(ffmpegError);
            }

            await hlsOutputPublisher.PublishAsync(storage, workspace.HlsDirectory, plan.HlsPrefix, cancellationToken);

            commandStore.AddWorkVideo(plan.ToWorkVideo(
                await commandStore.GetNextSortOrderAsync(request.WorkId, cancellationToken),
                DateTimeOffset.UtcNow));

            work.VideosVersion += 1;
            await commandStore.SaveChangesAsync(cancellationToken);
            return WorkVideoResult<WorkVideosMutationResult>.Ok(
                await queryStore.GetMutationResultAsync(request.WorkId, cancellationToken));
        }
    }
}
