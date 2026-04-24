namespace WoongBlog.Api.Modules.Identity.Login;

internal sealed class LoginRequest
{
    public string? ReturnUrl { get; init; } = "/admin";
}
