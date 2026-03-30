namespace WoongBlog.Api.Infrastructure.Auth;

public interface IAuthProfileLookupService
{
    Task<AuthProfileLookupResult?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public sealed record AuthProfileLookupResult(
    Guid Id,
    string Email,
    string DisplayName,
    string ProviderSubject);
