using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Modules.Content.Works.Application.CreateWork;

public sealed record CreateWorkCommand(
    string Title,
    string Category,
    string Period,
    string[] Tags,
    bool Published,
    string ContentJson,
    string AllPropertiesJson,
    Guid? ThumbnailAssetId,
    Guid? IconAssetId
) : IRequest<AdminMutationResult>;
