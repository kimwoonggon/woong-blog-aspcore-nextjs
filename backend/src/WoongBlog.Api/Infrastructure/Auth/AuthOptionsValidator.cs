using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Infrastructure.Auth;

internal sealed class AuthOptionsValidator(IHostEnvironment environment) : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.CookieName))
        {
            failures.Add("Auth:CookieName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DataProtectionKeysPath))
        {
            failures.Add("Auth:DataProtectionKeysPath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.MediaRoot))
        {
            failures.Add("Auth:MediaRoot is required.");
        }

        if (options.SlidingExpirationMinutes <= 0)
        {
            failures.Add("Auth:SlidingExpirationMinutes must be greater than 0.");
        }

        if (options.AbsoluteExpirationHours <= 0)
        {
            failures.Add("Auth:AbsoluteExpirationHours must be greater than 0.");
        }

        if (!IsLocalPath(options.CallbackPath))
        {
            failures.Add("Auth:CallbackPath must be a rooted local path.");
        }

        if (!IsLocalPath(options.SignedOutRedirectPath))
        {
            failures.Add("Auth:SignedOutRedirectPath must be a rooted local path.");
        }

        if (options.Scopes.Length == 0)
        {
            failures.Add("Auth:Scopes must contain at least one scope.");
        }

        if (options.Enabled
            && !environment.IsDevelopment()
            && !environment.IsEnvironment("Testing")
            && !options.IsConfigured())
        {
            failures.Add("Auth is enabled outside Development/Testing but ClientId/ClientSecret are not fully configured.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsLocalPath(string? path) => !string.IsNullOrWhiteSpace(path) && path.StartsWith('/');
}
