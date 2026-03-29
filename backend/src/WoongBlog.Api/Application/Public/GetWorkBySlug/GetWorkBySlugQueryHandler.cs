using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetWorkBySlug;

public class GetWorkBySlugQueryHandler : IRequestHandler<GetWorkBySlugQuery, WorkDetailDto?>
{
    private readonly IPublicWorkService _publicWorkService;

    public GetWorkBySlugQueryHandler(IPublicWorkService publicWorkService)
    {
        _publicWorkService = publicWorkService;
    }

    public async Task<WorkDetailDto?> Handle(GetWorkBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _publicWorkService.GetWorkBySlugAsync(request.Slug, cancellationToken);
    }
}
