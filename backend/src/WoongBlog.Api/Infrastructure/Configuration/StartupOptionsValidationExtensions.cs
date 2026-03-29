using Microsoft.Extensions.Options;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Proxy;
using WoongBlog.Api.Infrastructure.Security;

namespace WoongBlog.Api.Infrastructure.Configuration;

internal static class StartupOptionsValidationExtensions
{
    public static WebApplication ValidateStartupOptions(this WebApplication app)
    {
        _ = app.Services.GetRequiredService<IOptions<AuthOptions>>().Value;
        _ = app.Services.GetRequiredService<IOptions<AiOptions>>().Value;
        _ = app.Services.GetRequiredService<IOptions<ProxyOptions>>().Value;
        _ = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;

        return app;
    }
}
