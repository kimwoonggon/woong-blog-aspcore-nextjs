using MediatR;

namespace WoongBlog.Api.Application.Admin.GetAdminSiteSettings;

public sealed record GetAdminSiteSettingsQuery : IRequest<AdminSiteSettingsDto?>;
