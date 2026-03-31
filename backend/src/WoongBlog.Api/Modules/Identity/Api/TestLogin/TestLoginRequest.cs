namespace WoongBlog.Api.Modules.Identity.Api.TestLogin;

internal sealed class TestLoginRequest
{
    public string Email { get; init; } = "admin@example.com";
    public string ReturnUrl { get; init; } = "/admin";
}
