using MediatR;
using Microsoft.AspNetCore.Http;
using WoongBlog.Api.Domain.Entities;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Modules.Media.Application.Abstractions;
using WoongBlog.Api.Modules.Media.Application.Results;

namespace WoongBlog.Api.Modules.Media.Application.Commands.UploadMediaAsset;

public sealed class UploadMediaAssetCommandHandler(
    IMediaAssetCommandStore mediaAssetCommandStore,
    IMediaAssetStorage mediaAssetStorage,
    IMediaAssetUploadPolicy mediaAssetUploadPolicy) : IRequestHandler<UploadMediaAssetCommand, MediaUploadResult>
{
    private readonly IMediaAssetCommandStore _mediaAssetCommandStore = mediaAssetCommandStore;
    private readonly IMediaAssetStorage _mediaAssetStorage = mediaAssetStorage;
    private readonly IMediaAssetUploadPolicy _mediaAssetUploadPolicy = mediaAssetUploadPolicy;

    public async Task<MediaUploadResult> Handle(UploadMediaAssetCommand request, CancellationToken cancellationToken)
    {
        var validationError = _mediaAssetUploadPolicy.Validate(request.File);
        if (validationError is not null)
        {
            return new MediaUploadResult(false, StatusCodes.Status400BadRequest, validationError, null, null, null);
        }

        var file = request.File!;
        var plan = _mediaAssetUploadPolicy.BuildPlan(file, request.Bucket);

        try
        {
            await _mediaAssetStorage.SaveAsync(plan.RelativePath, file, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new MediaUploadResult(false, StatusCodes.Status500InternalServerError, ex.Message, null, null, null);
        }

        var profileIdValue = request.User.FindFirst(AuthClaimTypes.ProfileId)?.Value;
        Guid? profileId = Guid.TryParse(profileIdValue, out var parsed) ? parsed : null;

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Bucket = plan.Bucket,
            Path = plan.RelativePath,
            PublicUrl = plan.PublicUrl,
            MimeType = file.ContentType,
            Size = file.Length,
            Kind = _mediaAssetUploadPolicy.GetKind(file.ContentType),
            CreatedBy = profileId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _mediaAssetCommandStore.Add(asset);
        await _mediaAssetCommandStore.SaveChangesAsync(cancellationToken);

        return new MediaUploadResult(true, StatusCodes.Status200OK, null, asset.Id, asset.PublicUrl, asset.Path);
    }
}
