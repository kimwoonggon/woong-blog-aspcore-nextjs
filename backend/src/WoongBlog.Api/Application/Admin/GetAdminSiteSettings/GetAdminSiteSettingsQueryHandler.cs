using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminSiteSettings;

public sealed class GetAdminSiteSettingsQueryHandler : IRequestHandler<GetAdminSiteSettingsQuery, AdminSiteSettingsDto?>
{
    private readonly IAdminSiteSettingsQueries _adminSiteSettingsQueries;

    public GetAdminSiteSettingsQueryHandler(IAdminSiteSettingsQueries adminSiteSettingsQueries)
    {
        _adminSiteSettingsQueries = adminSiteSettingsQueries;
    }

    public async Task<AdminSiteSettingsDto?> Handle(GetAdminSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _adminSiteSettingsQueries.GetAsync(cancellationToken);
    }
}
