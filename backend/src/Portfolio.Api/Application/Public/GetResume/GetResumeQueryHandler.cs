using MediatR;
using Portfolio.Api.Application.Public.Abstractions;

namespace Portfolio.Api.Application.Public.GetResume;

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
