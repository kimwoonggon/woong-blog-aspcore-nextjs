using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using WoongBlog.Api.Endpoints;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Public.Abstractions;
using WoongBlog.Api.Application.Behaviors;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Infrastructure.Persistence.Admin;
using WoongBlog.Api.Infrastructure.Persistence.Public;
using WoongBlog.Api.Infrastructure.Proxy;
using WoongBlog.Api.Infrastructure.Security;
using WoongBlog.Api.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationExceptionFilter>();
});
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.PostConfigure<AiOptions>(options =>
{
    options.Provider = FirstConfigured(builder.Configuration["AI_PROVIDER"], options.Provider, "OpenAi");
    options.OpenAiApiKey = FirstConfigured(builder.Configuration["OPENAI_API_KEY"], options.OpenAiApiKey);
    options.OpenAiModel = FirstConfigured(builder.Configuration["OPENAI_MODEL"], options.OpenAiModel, "gpt-4o");
    options.AzureOpenAiApiKey = FirstConfigured(builder.Configuration["AZURE_OPENAI_API_KEY"], options.AzureOpenAiApiKey);
    options.AzureOpenAiEndpoint = FirstConfigured(builder.Configuration["AZURE_OPENAI_ENDPOINT"], options.AzureOpenAiEndpoint);
    options.AzureOpenAiDeployment = FirstConfigured(
        builder.Configuration["AZURE_OPENAI_DEPLOYMENT"],
        builder.Configuration["AZURE_DEPLOYMENT_NAME"],
        options.AzureOpenAiDeployment,
        "gpt-5.2-chat");
    options.AzureOpenAiApiVersion = FirstConfigured(builder.Configuration["AZURE_OPENAI_API_VERSION"], options.AzureOpenAiApiVersion, "2024-08-01-preview");
    options.CodexCommand = FirstConfigured(builder.Configuration["CODEX_COMMAND"], options.CodexCommand, "codex");
    options.CodexModel = FirstConfigured(builder.Configuration["CODEX_MODEL"], options.CodexModel, "gpt-5.4");
    options.CodexReasoningEffort = FirstConfigured(builder.Configuration["CODEX_REASONING_EFFORT"], options.CodexReasoningEffort, "medium");
    if (int.TryParse(builder.Configuration["CODEX_TIMEOUT_MS"], out var timeoutMs) && timeoutMs > 0)
    {
        options.CodexTimeoutMs = timeoutMs;
    }
    options.CodexWorkdir = FirstConfigured(builder.Configuration["CODEX_WORKDIR"], options.CodexWorkdir);
    if (int.TryParse(builder.Configuration["AI_BATCH_CONCURRENCY"], out var batchConcurrency) && batchConcurrency > 0)
    {
        options.BatchConcurrency = batchConcurrency;
    }
    if (int.TryParse(builder.Configuration["AI_BATCH_COMPLETED_RETENTION_DAYS"], out var retentionDays) && retentionDays >= 0)
    {
        options.BatchCompletedRetentionDays = retentionDays;
    }
    options.CodexAllowedModels = ParseCsv(
        builder.Configuration["CODEX_ALLOWED_MODELS"],
        options.CodexAllowedModels,
        ["gpt-5.4", "gpt-5.3-codex", "gpt-5.3-codex-spark"]);
    options.CodexAllowedReasoningEfforts = ParseCsv(
        builder.Configuration["CODEX_ALLOWED_REASONING_EFFORTS"],
        options.CodexAllowedReasoningEfforts,
        ["low", "medium", "high", "xhigh"]);
});
builder.Services.Configure<ProxyOptions>(builder.Configuration.GetSection(ProxyOptions.SectionName));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
var proxyOptions = builder.Configuration.GetSection(ProxyOptions.SectionName).Get<ProxyOptions>() ?? new ProxyOptions();
var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
var antiforgerySecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
    ? CookieSecurePolicy.None
    : authOptions.SecureCookies
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;

Directory.CreateDirectory(authOptions.DataProtectionKeysPath);
Directory.CreateDirectory(authOptions.MediaRoot);

builder.Services.AddDbContext<WoongBlogDbContext>(options =>
{
    var databaseProvider = builder.Configuration["DatabaseProvider"];

    if (string.Equals(databaseProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
    {
        var inMemoryDatabaseName = builder.Configuration["InMemoryDatabaseName"] ?? "portfolio-tests";
        options.UseInMemoryDatabase(inMemoryDatabaseName);
        return;
    }

    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio";

    options.UseNpgsql(connectionString);
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped<AuthRecorder>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminMemberService, AdminMemberService>();
builder.Services.AddScoped<IAdminPageService, AdminPageService>();
builder.Services.AddScoped<IAdminSiteSettingsService, AdminSiteSettingsService>();
builder.Services.AddScoped<IAdminBlogService, AdminBlogService>();
builder.Services.AddScoped<IAdminWorkService, AdminWorkService>();
builder.Services.AddScoped<IPublicHomeService, PublicHomeService>();
builder.Services.AddScoped<IPublicPageService, PublicPageService>();
builder.Services.AddScoped<IPublicSiteService, PublicSiteService>();
builder.Services.AddScoped<IPublicBlogService, PublicBlogService>();
builder.Services.AddScoped<IPublicWorkService, PublicWorkService>();
builder.Services.AddHttpClient<IBlogAiFixService, BlogAiFixService>();
builder.Services.AddHostedService<AiBatchJobProcessor>();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(authOptions.DataProtectionKeysPath))
    .SetApplicationName("WoongBlog.Api");
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = securityOptions.AntiforgeryHeaderName;
    options.Cookie.Name = securityOptions.AntiforgeryCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = antiforgerySecurePolicy;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = securityOptions.AuthRateLimitPermitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(securityOptions.AuthRateLimitWindowSeconds);
        limiterOptions.QueueLimit = 0;
    });
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(securityOptions.HstsMaxAgeDays);
    options.IncludeSubDomains = securityOptions.HstsIncludeSubDomains;
    options.Preload = securityOptions.HstsPreload;
});

var authenticationBuilder = builder.Services
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
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect("/login?reason=session_expired");
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = async context =>
            {
                var recorder = context.HttpContext.RequestServices.GetRequiredService<AuthRecorder>();
                await recorder.RecordDeniedAccessAsync(context.HttpContext.User, context.HttpContext, "access_denied", context.HttpContext.RequestAborted);

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                context.Response.Redirect("/login?reason=access_denied");
            },
            OnValidatePrincipal = async context =>
            {
                var recorder = context.HttpContext.RequestServices.GetRequiredService<AuthRecorder>();
                var isValid = await recorder.ValidateSessionAsync(context.Principal!, context.HttpContext.RequestAborted);
                if (!isValid)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
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

        options.Scope.Clear();
        foreach (var scope in authOptions.Scopes)
        {
            options.Scope.Add(scope);
        }

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var recorder = context.HttpContext.RequestServices.GetRequiredService<AuthRecorder>();
                var result = await recorder.RecordSuccessfulLoginAsync(context.Principal!, context.HttpContext, context.HttpContext.RequestAborted);
                context.Properties ??= new AuthenticationProperties();

                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    identity.AddClaim(new Claim(AuthClaimTypes.ProfileId, result.ProfileId.ToString()));
                    identity.AddClaim(new Claim(AuthClaimTypes.Role, result.Role));
                    identity.AddClaim(new Claim(AuthClaimTypes.SessionId, result.SessionId.ToString()));
                    identity.AddClaim(new Claim(ClaimTypes.Role, result.Role));
                    if (!identity.HasClaim(x => x.Type == ClaimTypes.Email))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Email, result.Email));
                    }
                }

                context.Properties.IssuedUtc = DateTimeOffset.UtcNow;
                context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(authOptions.SlidingExpirationMinutes);
                context.Properties.AllowRefresh = true;
            },
            OnRemoteFailure = async context =>
            {
                var recorder = context.HttpContext.RequestServices.GetRequiredService<AuthRecorder>();
                await recorder.RecordLoginFailureAsync(context.HttpContext, context.Failure?.Message ?? "OIDC remote failure", context.HttpContext.RequestAborted);
                context.Response.Redirect("/login?error=auth_failed");
                context.HandleResponse();
            }
        };
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim(AuthClaimTypes.Role, "admin"));
});

var app = builder.Build();

var forwardedHeadersOptions = ForwardedHeadersOptionsFactory.Create(proxyOptions);
app.UseForwardedHeaders(forwardedHeadersOptions);

if (securityOptions.UseHttpsRedirection)
{
    app.UseHttpsRedirection();
}

if (securityOptions.UseHsts && !app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WoongBlogDbContext>();
    await DatabaseBootstrapper.InitializeAsync(dbContext);
}

app.UseAuthentication();
app.UseMiddleware<AntiforgeryValidationMiddleware>();
app.UseAuthorization();

app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/media",
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(authOptions.MediaRoot)
});

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi("/api/openapi/v1.json");
}

app.MapControllers();
app.MapAdminAiEndpoints();
app.MapGet("/", () => Results.Redirect("/api/health"));

app.Run();

static string FirstConfigured(params string?[] values)
{
    foreach (var value in values)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return string.Empty;
}

static string[] ParseCsv(string? raw, IReadOnlyList<string> current, IReadOnlyList<string> fallback)
{
    if (!string.IsNullOrWhiteSpace(raw))
    {
        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    if (current.Count > 0)
    {
        return current.ToArray();
    }

    return fallback.ToArray();
}

public partial class Program
{
}

internal static class ForwardedHeadersOptionsFactory
{
    public static ForwardedHeadersOptions Create(ProxyOptions proxyOptions)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            ForwardLimit = proxyOptions.ForwardLimit,
            RequireHeaderSymmetry = proxyOptions.RequireHeaderSymmetry
        };

        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        if (proxyOptions.KnownProxies.Length == 0 && proxyOptions.KnownNetworks.Length == 0)
        {
            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
            return options;
        }

        foreach (var knownProxy in proxyOptions.KnownProxies)
        {
            if (IPAddress.TryParse(knownProxy, out var proxyAddress))
            {
                options.KnownProxies.Add(proxyAddress);
            }
        }

        foreach (var knownNetwork in proxyOptions.KnownNetworks)
        {
            var parts = knownNetwork.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2
                && IPAddress.TryParse(parts[0], out var prefixAddress)
                && int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse($"{prefixAddress}/{prefixLength}"));
            }
        }

        return options;
    }
}
