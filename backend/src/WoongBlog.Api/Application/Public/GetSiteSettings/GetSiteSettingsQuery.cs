using MediatR;

namespace WoongBlog.Api.Application.Public.GetSiteSettings;

public sealed record GetSiteSettingsQuery : IRequest<SiteSettingsDto?>;
