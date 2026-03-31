using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Works.Application.UpdateWork;

public sealed record UpdateWorkCommand(
    Guid Id,
    string Title,
    string Category,
    string Period,
    string[] Tags,
    bool Published,
    string ContentJson,
    string AllPropertiesJson,
    Guid? ThumbnailAssetId,
    Guid? IconAssetId
) : IRequest<AdminMutationResult?>;
