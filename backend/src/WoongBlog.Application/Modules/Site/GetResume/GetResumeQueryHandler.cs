using MediatR;
using WoongBlog.Application.Modules.Site.Abstractions;

namespace WoongBlog.Application.Modules.Site.GetResume;

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
