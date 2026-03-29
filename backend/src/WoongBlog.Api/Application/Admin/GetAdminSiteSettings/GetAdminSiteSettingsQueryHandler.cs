using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.GetAdminSiteSettings;

public sealed class GetAdminSiteSettingsQueryHandler : IRequestHandler<GetAdminSiteSettingsQuery, AdminSiteSettingsDto?>
{
    private readonly IAdminSiteSettingsService _adminSiteSettingsService;

    public GetAdminSiteSettingsQueryHandler(IAdminSiteSettingsService adminSiteSettingsService)
    {
        _adminSiteSettingsService = adminSiteSettingsService;
    }

    public async Task<AdminSiteSettingsDto?> Handle(GetAdminSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _adminSiteSettingsService.GetAsync(cancellationToken);
    }
}
