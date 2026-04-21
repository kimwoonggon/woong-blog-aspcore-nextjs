using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Auth;

namespace WoongBlog.Api.Infrastructure.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecurityOptions>, SecurityOptionsValidator>();

        var securityOptions = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var antiforgerySecurePolicy = ResolveAntiforgerySecurePolicy(environment, authOptions);

        services.AddAntiforgery(options =>
        {
            options.HeaderName = securityOptions.AntiforgeryHeaderName;
            options.Cookie.Name = securityOptions.AntiforgeryCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = antiforgerySecurePolicy;
        });

        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(securityOptions.HstsMaxAgeDays);
            options.IncludeSubDomains = securityOptions.HstsIncludeSubDomains;
            options.Preload = securityOptions.HstsPreload;
        });

        return services;
    }

    public static WebApplication UseConfiguredTransportSecurity(this WebApplication app)
    {
        var securityOptions = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;

        if (securityOptions.UseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        if (securityOptions.UseHsts && !app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        return app;
    }

    private static CookieSecurePolicy ResolveAntiforgerySecurePolicy(IHostEnvironment environment, AuthOptions authOptions)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return CookieSecurePolicy.None;
        }

        return authOptions.SecureCookies
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
    }
}
