namespace WoongBlog.Api.Infrastructure.Security;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public bool UseHttpsRedirection { get; set; } = true;
    public bool UseHsts { get; set; } = true;
    public int HstsMaxAgeDays { get; set; } = 365;
    public bool HstsIncludeSubDomains { get; set; } = true;
    public bool HstsPreload { get; set; }
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public string PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=()";
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self' https://accounts.google.com; img-src 'self' data: https: blob:; object-src 'none'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; connect-src 'self' https://accounts.google.com https://www.googleapis.com;";
    public int AuthRateLimitPermitLimit { get; set; } = 20;
    public int AuthRateLimitWindowSeconds { get; set; } = 60;
    public string AntiforgeryHeaderName { get; set; } = "X-CSRF-TOKEN";
    public string AntiforgeryCookieName { get; set; } = "portfolio_xsrf";
}
