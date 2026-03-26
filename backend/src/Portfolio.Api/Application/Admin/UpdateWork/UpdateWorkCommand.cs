using MediatR;
using Portfolio.Api.Application.Admin.Support;

namespace Portfolio.Api.Application.Admin.UpdateWork;

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
