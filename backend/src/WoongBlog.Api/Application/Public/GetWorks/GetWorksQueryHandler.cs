using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetWorks;

public class GetWorksQueryHandler : IRequestHandler<GetWorksQuery, PagedWorksDto>
{
    private readonly IPublicWorkService _publicWorkService;

    public GetWorksQueryHandler(IPublicWorkService publicWorkService)
    {
        _publicWorkService = publicWorkService;
    }

    public async Task<PagedWorksDto> Handle(GetWorksQuery request, CancellationToken cancellationToken)
    {
        return await _publicWorkService.GetWorksAsync(request.Page, request.PageSize, cancellationToken);
    }
}
