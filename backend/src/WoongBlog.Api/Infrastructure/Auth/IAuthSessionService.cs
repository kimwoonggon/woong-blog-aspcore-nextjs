using System.Security.Claims;

namespace WoongBlog.Api.Infrastructure.Auth;

public interface IAuthSessionService
{
    Task<AuthRecordResult> RecordSuccessfulLoginAsync(
        ClaimsPrincipal principal,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task RevokeCurrentSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);
}
