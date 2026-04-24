using MediatR;

namespace WoongBlog.Application.Modules.Site.GetAdminSiteSettings;

public sealed record GetAdminSiteSettingsQuery : IRequest<AdminSiteSettingsDto?>;
