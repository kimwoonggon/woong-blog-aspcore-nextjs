using MediatR;

namespace WoongBlog.Application.Modules.Site.GetSiteSettings;

public sealed record GetSiteSettingsQuery : IRequest<SiteSettingsDto?>;
