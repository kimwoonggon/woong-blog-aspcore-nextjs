using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetWorks;

public class GetWorksQueryHandler : IRequestHandler<GetWorksQuery, PagedWorksDto>
{
    private readonly IPublicWorkQueries _publicWorkQueries;

    public GetWorksQueryHandler(IPublicWorkQueries publicWorkQueries)
    {
        _publicWorkQueries = publicWorkQueries;
    }

    public async Task<PagedWorksDto> Handle(GetWorksQuery request, CancellationToken cancellationToken)
    {
        return await _publicWorkQueries.GetWorksAsync(request.Page, request.PageSize, cancellationToken);
    }
}
