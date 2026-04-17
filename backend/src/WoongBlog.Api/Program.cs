using Microsoft.AspNetCore.HttpOverrides;
using WoongBlog.Api.Common;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Configuration;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Modules.AI.Api;
using WoongBlog.Api.Modules.AI;
using WoongBlog.Api.Modules.Composition.Api;
using WoongBlog.Api.Modules.Composition;
using WoongBlog.Api.Modules.Content.Blogs.Api;
using WoongBlog.Api.Modules.Content.Blogs;
using WoongBlog.Api.Modules.Content.Pages.Api;
using WoongBlog.Api.Modules.Content.Pages;
using WoongBlog.Api.Modules.Content.Works.Api;
using WoongBlog.Api.Modules.Content.Works;
using WoongBlog.Api.Modules.Identity.Api;
using WoongBlog.Api.Modules.Identity;
using WoongBlog.Api.Modules.Media.Api;
using WoongBlog.Api.Modules.Media;
using WoongBlog.Api.Modules.Site.Api;
using WoongBlog.Api.Modules.Site;
using WoongBlog.Api.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommonModule(builder.Configuration, builder.Environment);
builder.Services.AddPagesModule();
builder.Services.AddBlogsModule();
builder.Services.AddWorksModule(builder.Configuration);
builder.Services.AddSiteModule();
builder.Services.AddCompositionModule();
builder.Services.AddIdentityModule(builder.Configuration, builder.Environment);
builder.Services.AddMediaModule();
builder.Services.AddAiModule(builder.Configuration);

var app = builder.Build();

app.ValidateStartupOptions();
app.UseForwardedHeaders();
app.UseConfiguredTransportSecurity();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.EnsureAuthStorageDirectories();
await app.InitializeDatabaseAsync();
app.UseAuthentication();
app.UseMiddleware<AntiforgeryValidationMiddleware>();
app.UseAuthorization();
app.UseMediaStaticFiles();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi("/api/openapi/v1.json");
}

app.MapControllers();
app.MapPagesModule();
app.MapBlogsModule();
app.MapWorksModule();
app.MapSiteModule();
app.MapCompositionModule();
app.MapIdentityModule();
app.MapMediaModule();
app.MapAiModule();
app.MapGet("/", () => Results.Redirect("/api/health"));

app.Run();

public partial class Program
{
}
