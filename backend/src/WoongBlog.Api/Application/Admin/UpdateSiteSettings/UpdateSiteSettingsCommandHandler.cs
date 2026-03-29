using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdateSiteSettings;

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
