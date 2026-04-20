using MediatR;
using WoongBlog.Api.Modules.Media.Application.Results;

namespace WoongBlog.Api.Modules.Media.Application.Commands.DeleteMediaAsset;

public sealed record DeleteMediaAssetCommand(Guid Id) : IRequest<MediaDeleteResult>;
