namespace WoongBlog.Api.Modules.Identity.Api.Login;

internal sealed class LoginRequest
{
    public string? ReturnUrl { get; init; } = "/admin";
}
