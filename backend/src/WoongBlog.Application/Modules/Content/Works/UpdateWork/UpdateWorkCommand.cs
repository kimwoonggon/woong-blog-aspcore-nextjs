using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;

namespace WoongBlog.Application.Modules.Content.Works.UpdateWork;

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
