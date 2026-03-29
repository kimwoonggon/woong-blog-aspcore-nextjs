using Microsoft.AspNetCore.HttpOverrides;
using WoongBlog.Api.Application;
using WoongBlog.Api.Infrastructure.Configuration;
using WoongBlog.Api.Endpoints;
using WoongBlog.Api.Infrastructure.Ai;
using WoongBlog.Api.Infrastructure.Auth;
using WoongBlog.Api.Infrastructure.Persistence;
using WoongBlog.Api.Infrastructure.Proxy;
using WoongBlog.Api.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiCore();
builder.Services.AddProxyInfrastructure(builder.Configuration);
builder.Services.AddSecurityInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddAuthInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddAiInfrastructure(builder.Configuration);

var app = builder.Build();

app.ValidateStartupOptions();
app.UseForwardedHeaders();
app.UseConfiguredTransportSecurity();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRateLimiter();
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
app.MapAdminAiEndpoints();
app.MapGet("/", () => Results.Redirect("/api/health"));

app.Run();

public partial class Program
{
}
