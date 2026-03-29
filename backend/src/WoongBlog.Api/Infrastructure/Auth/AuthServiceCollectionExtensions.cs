using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Infrastructure.Auth;

internal static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AuthOptions>>(_ => new AuthOptionsValidator(environment));

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        services.AddScoped<AuthRecorder>();
        services.AddScoped<AppCookieAuthenticationEvents>();
        services.AddScoped<AppOpenIdConnectEvents>();

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(authOptions.DataProtectionKeysPath))
            .SetApplicationName("WoongBlog.Api");

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = authOptions.IsConfigured()
                    ? OpenIdConnectDefaults.AuthenticationScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = authOptions.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = authOptions.SecureCookies
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(authOptions.SlidingExpirationMinutes);
                options.SlidingExpiration = true;
                options.LoginPath = "/login";
                options.EventsType = typeof(AppCookieAuthenticationEvents);
            });

        if (authOptions.IsConfigured())
        {
            authenticationBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authOptions.Authority;
                options.ClientId = authOptions.ClientId;
                options.ClientSecret = authOptions.ClientSecret;
                options.CallbackPath = authOptions.CallbackPath;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
                options.EventsType = typeof(AppOpenIdConnectEvents);

                options.Scope.Clear();
                foreach (var scope in authOptions.Scopes)
                {
                    options.Scope.Add(scope);
                }
            });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireClaim(AuthClaimTypes.Role, "admin"));
        });

        return services;
    }
}
