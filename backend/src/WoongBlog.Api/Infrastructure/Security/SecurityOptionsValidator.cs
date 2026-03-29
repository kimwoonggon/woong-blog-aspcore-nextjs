using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Infrastructure.Security;

internal sealed class SecurityOptionsValidator : IValidateOptions<SecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        var failures = new List<string>();

        if (options.HstsMaxAgeDays < 0)
        {
            failures.Add("Security:HstsMaxAgeDays must be 0 or greater.");
        }

        if (options.AuthRateLimitPermitLimit <= 0)
        {
            failures.Add("Security:AuthRateLimitPermitLimit must be greater than 0.");
        }

        if (options.AuthRateLimitWindowSeconds <= 0)
        {
            failures.Add("Security:AuthRateLimitWindowSeconds must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(options.ReferrerPolicy))
        {
            failures.Add("Security:ReferrerPolicy is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PermissionsPolicy))
        {
            failures.Add("Security:PermissionsPolicy is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ContentSecurityPolicy))
        {
            failures.Add("Security:ContentSecurityPolicy is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AntiforgeryHeaderName))
        {
            failures.Add("Security:AntiforgeryHeaderName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AntiforgeryCookieName))
        {
            failures.Add("Security:AntiforgeryCookieName is required.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
