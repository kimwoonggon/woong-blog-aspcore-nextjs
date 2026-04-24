using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.StaticFiles;

namespace WoongBlog.Infrastructure.Auth;

public static class AuthRuntimeExtensions
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
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".m3u8"] = "application/vnd.apple.mpegurl";
        contentTypeProvider.Mappings[".ts"] = "video/mp2t";
        contentTypeProvider.Mappings[".vtt"] = "text/vtt";

        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/media",
            FileProvider = new PhysicalFileProvider(authOptions.MediaRoot),
            ContentTypeProvider = contentTypeProvider
        });

        return app;
    }
}
