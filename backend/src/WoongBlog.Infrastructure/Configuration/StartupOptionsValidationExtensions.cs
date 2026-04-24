using Microsoft.Extensions.Options;
using WoongBlog.Application.Modules.AI;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Infrastructure.Proxy;
using WoongBlog.Infrastructure.Security;

namespace WoongBlog.Infrastructure.Configuration;

public static class StartupOptionsValidationExtensions
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
