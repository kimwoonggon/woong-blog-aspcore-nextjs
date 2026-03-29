using System.Security.Claims;

namespace WoongBlog.Api.Infrastructure.Auth;

public interface IAuthAuditService
{
    Task RecordLoginFailureAsync(HttpContext httpContext, string reason, CancellationToken cancellationToken = default);

    Task RecordLogoutAsync(ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken = default);

    Task RecordDeniedAccessAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        string reason,
        CancellationToken cancellationToken = default);
}
