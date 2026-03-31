using MediatR;
using WoongBlog.Api.Modules.Content.Works.Application.Abstractions;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

public class GetWorksQueryHandler : IRequestHandler<GetWorksQuery, PagedWorksDto>
{
    private readonly IPublicWorkService _publicWorkService;

    public GetWorksQueryHandler(IPublicWorkService publicWorkService)
    {
        _publicWorkService = publicWorkService;
    }

    public async Task<PagedWorksDto> Handle(GetWorksQuery request, CancellationToken cancellationToken)
    {
        return await _publicWorkService.GetWorksAsync(request, cancellationToken);
    }
}
