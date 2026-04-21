using MediatR;

namespace WoongBlog.Api.Modules.Site.Application.GetSiteSettings;

public sealed record GetSiteSettingsQuery : IRequest<SiteSettingsDto?>;
