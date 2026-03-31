using MediatR;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Application.GetResume;

public class GetResumeQueryHandler : IRequestHandler<GetResumeQuery, ResumeDto?>
{
    private readonly IPublicSiteService _publicSiteService;

    public GetResumeQueryHandler(IPublicSiteService publicSiteService)
    {
        _publicSiteService = publicSiteService;
    }

    public async Task<ResumeDto?> Handle(GetResumeQuery request, CancellationToken cancellationToken)
    {
        return await _publicSiteService.GetResumeAsync(cancellationToken);
    }
}
