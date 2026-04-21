using MediatR;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Application.GetResume;

public class GetResumeQueryHandler : IRequestHandler<GetResumeQuery, ResumeDto?>
{
    private readonly ISiteSettingsQueryStore _siteSettingsQueryStore;

    public GetResumeQueryHandler(ISiteSettingsQueryStore siteSettingsQueryStore)
    {
        _siteSettingsQueryStore = siteSettingsQueryStore;
    }

    public async Task<ResumeDto?> Handle(GetResumeQuery request, CancellationToken cancellationToken)
    {
        return await _siteSettingsQueryStore.GetResumeAsync(cancellationToken);
    }
}
