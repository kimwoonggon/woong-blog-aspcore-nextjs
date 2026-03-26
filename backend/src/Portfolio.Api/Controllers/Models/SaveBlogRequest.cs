namespace Portfolio.Api.Controllers.Models;

public class SaveBlogRequest
{
    public string Title { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public bool Published { get; set; }
    public string ContentJson { get; set; } = "{}";
}
