using MediatR;

namespace Portfolio.Api.Application.Admin.GetAdminSiteSettings;

public sealed record GetAdminSiteSettingsQuery : IRequest<AdminSiteSettingsDto?>;
