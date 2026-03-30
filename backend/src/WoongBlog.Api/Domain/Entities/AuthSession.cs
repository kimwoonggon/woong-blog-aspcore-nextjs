namespace WoongBlog.Api.Domain.Entities;

public class AuthSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProfileId { get; private set; }
    public string SessionKey { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static AuthSession Create(Guid profileId, string ipAddress, string userAgent, DateTimeOffset now, DateTimeOffset? expiresAt)
    {
        return new AuthSession
        {
            ProfileId = profileId,
            SessionKey = Guid.NewGuid().ToString("N"),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = expiresAt
        };
    }

    public static AuthSession Seed(Guid id, Guid profileId, string sessionKey, DateTimeOffset createdAt, DateTimeOffset lastSeenAt, DateTimeOffset? expiresAt, DateTimeOffset? revokedAt = null, string ipAddress = "", string userAgent = "")
    {
        return new AuthSession
        {
            Id = id,
            ProfileId = profileId,
            SessionKey = sessionKey,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = createdAt,
            LastSeenAt = lastSeenAt,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt
        };
    }

    public void RecordSeen(DateTimeOffset now)
    {
        LastSeenAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt = now;
        LastSeenAt = now;
    }
}
