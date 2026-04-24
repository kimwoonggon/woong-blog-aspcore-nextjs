using MediatR;
using WoongBlog.Application.Modules.Media.Abstractions;
using WoongBlog.Application.Modules.Media.Results;

namespace WoongBlog.Application.Modules.Media.Commands.DeleteMediaAsset;

public sealed class DeleteMediaAssetCommandHandler(
    IMediaAssetCommandStore mediaAssetCommandStore,
    IMediaAssetStorage mediaAssetStorage) : IRequestHandler<DeleteMediaAssetCommand, MediaDeleteResult>
{
    private readonly IMediaAssetCommandStore _mediaAssetCommandStore = mediaAssetCommandStore;
    private readonly IMediaAssetStorage _mediaAssetStorage = mediaAssetStorage;

    public async Task<MediaDeleteResult> Handle(DeleteMediaAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _mediaAssetCommandStore.GetByIdAsync(request.Id, cancellationToken);
        if (asset is null)
        {
            return new MediaDeleteResult(false);
        }

        await _mediaAssetStorage.DeleteAsync(asset.Path, cancellationToken);
        _mediaAssetCommandStore.Remove(asset);
        await _mediaAssetCommandStore.SaveChangesAsync(cancellationToken);
        return new MediaDeleteResult(true);
    }
}
