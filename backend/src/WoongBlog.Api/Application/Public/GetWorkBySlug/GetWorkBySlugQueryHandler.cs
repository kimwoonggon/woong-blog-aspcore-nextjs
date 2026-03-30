using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetWorkBySlug;

public class GetWorkBySlugQueryHandler : IRequestHandler<GetWorkBySlugQuery, WorkDetailDto?>
{
    private readonly IPublicWorkQueries _publicWorkQueries;

    public GetWorkBySlugQueryHandler(IPublicWorkQueries publicWorkQueries)
    {
        _publicWorkQueries = publicWorkQueries;
    }

    public async Task<WorkDetailDto?> Handle(GetWorkBySlugQuery request, CancellationToken cancellationToken)
    {
        return await _publicWorkQueries.GetWorkBySlugAsync(request.Slug, cancellationToken);
    }
}
