using MediatR;

namespace WoongBlog.Api.Modules.Site.Application.GetAdminSiteSettings;

public sealed record GetAdminSiteSettingsQuery : IRequest<AdminSiteSettingsDto?>;
