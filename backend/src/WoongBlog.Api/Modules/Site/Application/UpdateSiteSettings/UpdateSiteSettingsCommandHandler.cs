using MediatR;
using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Site.Application.Abstractions;

namespace WoongBlog.Api.Modules.Site.Application.UpdateSiteSettings;

public sealed class UpdateSiteSettingsCommandHandler : IRequestHandler<UpdateSiteSettingsCommand, AdminActionResult>
{
    private readonly IAdminSiteSettingsService _adminSiteSettingsService;

    public UpdateSiteSettingsCommandHandler(IAdminSiteSettingsService adminSiteSettingsService)
    {
        _adminSiteSettingsService = adminSiteSettingsService;
    }

    public async Task<AdminActionResult> Handle(UpdateSiteSettingsCommand request, CancellationToken cancellationToken)
    {
        return await _adminSiteSettingsService.UpdateAsync(request, cancellationToken);
    }
}
