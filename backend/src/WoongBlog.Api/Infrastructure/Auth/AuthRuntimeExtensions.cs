using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Infrastructure.Auth;

internal static class AuthRuntimeExtensions
{
    public static WebApplication EnsureAuthStorageDirectories(this WebApplication app)
    {
        var authOptions = app.Services.GetRequiredService<IOptions<AuthOptions>>().Value;
        Directory.CreateDirectory(authOptions.DataProtectionKeysPath);
        Directory.CreateDirectory(authOptions.MediaRoot);

        return app;
    }

    public static WebApplication UseMediaStaticFiles(this WebApplication app)
    {
        var authOptions = app.Services.GetRequiredService<IOptions<AuthOptions>>().Value;

        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/media",
            FileProvider = new PhysicalFileProvider(authOptions.MediaRoot)
        });

        return app;
    }
}
