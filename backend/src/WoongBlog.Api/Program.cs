using Microsoft.AspNetCore.HttpOverrides;
using WoongBlog.Api.Application;
using WoongBlog.Api.Common;
using WoongBlog.Infrastructure.Auth;
using WoongBlog.Infrastructure.Configuration;
using WoongBlog.Infrastructure;
using WoongBlog.Infrastructure.Persistence;
using WoongBlog.Api.Modules.AI;
using WoongBlog.Api.Modules.Composition;
using WoongBlog.Api.Modules.Content.Blogs;
using WoongBlog.Api.Modules.Content.Pages;
using WoongBlog.Api.Modules.Content.Works;
using WoongBlog.Api.Modules.Identity;
using WoongBlog.Api.Modules.Media;
using WoongBlog.Api.Modules.Site;
using WoongBlog.Infrastructure.Security;
using WoongBlog.Infrastructure.Modules.AI;
using WoongBlog.Infrastructure.Modules.Composition;
using WoongBlog.Infrastructure.Modules.Content.Blogs;
using WoongBlog.Infrastructure.Modules.Content.Pages;
using WoongBlog.Infrastructure.Modules.Content.Works;
using WoongBlog.Infrastructure.Modules.Identity;
using WoongBlog.Infrastructure.Modules.Media;
using WoongBlog.Infrastructure.Modules.Site;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiCore();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
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
