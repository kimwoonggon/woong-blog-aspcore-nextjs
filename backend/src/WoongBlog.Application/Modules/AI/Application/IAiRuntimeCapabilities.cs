namespace WoongBlog.Api.Modules.AI.Application;

public interface IAiRuntimeCapabilities
{
    IReadOnlyList<string> GetAvailableProviders();
    string GetDefaultBlogFixPrompt();
    string GetDefaultWorkEnrichPrompt();
}
