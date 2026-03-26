namespace Portfolio.Api.Infrastructure.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public bool Enabled { get; set; }
    public string Authority { get; set; } = "https://accounts.google.com";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/api/auth/callback";
    public string SignedOutRedirectPath { get; set; } = "/";
    public string CookieName { get; set; } = "portfolio_auth";
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];
    public string[] AdminEmails { get; set; } = [];
    public string DataProtectionKeysPath { get; set; } = "/app/data-protection";
    public string MediaRoot { get; set; } = "/app/media";
    public int SlidingExpirationMinutes { get; set; } = 20;
    public int AbsoluteExpirationHours { get; set; } = 8;
    public bool SecureCookies { get; set; }
    public bool RequireHttpsMetadata { get; set; }

    public bool IsConfigured()
    {
        return Enabled
               && !string.IsNullOrWhiteSpace(ClientId)
               && !string.IsNullOrWhiteSpace(ClientSecret)
               && !ClientId.Contains("your-", StringComparison.OrdinalIgnoreCase)
               && !ClientSecret.Contains("your-", StringComparison.OrdinalIgnoreCase);
    }
}
