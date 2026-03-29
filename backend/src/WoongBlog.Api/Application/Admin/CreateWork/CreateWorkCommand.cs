using MediatR;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.CreateWork;

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
