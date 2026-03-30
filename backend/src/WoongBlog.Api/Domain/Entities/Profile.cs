namespace WoongBlog.Api.Domain.Entities;

public class Profile
{
    public Guid Id { get; private set; }
    public string Provider { get; private set; } = "google";
    public string ProviderSubject { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Role { get; private set; } = "user";
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static Profile CreateGoogleProfile(string providerSubject, string email, string displayName, string role, DateTimeOffset now)
    {
        return new Profile
        {
            Id = Guid.NewGuid(),
            Provider = "google",
            ProviderSubject = providerSubject,
            Email = email,
            DisplayName = displayName,
            Role = role,
            CreatedAt = now,
            UpdatedAt = now,
            LastLoginAt = now
        };
    }

    public static Profile Seed(Guid id, string email, string displayName, string provider, string providerSubject, string role, DateTimeOffset? createdAt = null, DateTimeOffset? updatedAt = null, DateTimeOffset? lastLoginAt = null)
    {
        return new Profile
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            Provider = provider,
            ProviderSubject = providerSubject,
            Role = role,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow,
            LastLoginAt = lastLoginAt
        };
    }

    public void RefreshGoogleLogin(string providerSubject, string email, string displayName, DateTimeOffset now)
    {
        Provider = "google";
        ProviderSubject = providerSubject;
        Email = email;
        DisplayName = displayName;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public void EnsureRole(string role)
    {
        if (string.IsNullOrWhiteSpace(Role))
        {
            Role = role;
        }
    }
}
