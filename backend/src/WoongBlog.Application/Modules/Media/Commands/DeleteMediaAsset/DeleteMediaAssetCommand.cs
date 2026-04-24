using MediatR;
using WoongBlog.Application.Modules.Media.Results;

namespace WoongBlog.Application.Modules.Media.Commands.DeleteMediaAsset;

public sealed record DeleteMediaAssetCommand(Guid Id) : IRequest<MediaDeleteResult>;
