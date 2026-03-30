using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetResume;

public class GetResumeQueryHandler : IRequestHandler<GetResumeQuery, ResumeDto?>
{
    private readonly IPublicSiteQueries _publicSiteQueries;

    public GetResumeQueryHandler(IPublicSiteQueries publicSiteQueries)
    {
        _publicSiteQueries = publicSiteQueries;
    }

    public async Task<ResumeDto?> Handle(GetResumeQuery request, CancellationToken cancellationToken)
    {
        return await _publicSiteQueries.GetResumeAsync(cancellationToken);
    }
}
