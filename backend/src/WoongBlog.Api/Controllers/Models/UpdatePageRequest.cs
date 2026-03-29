namespace WoongBlog.Api.Controllers.Models;

public class UpdatePageRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
}
