using MediatR;

namespace Portfolio.Api.Application.Public.GetSiteSettings;

public sealed record GetSiteSettingsQuery : IRequest<SiteSettingsDto?>;
