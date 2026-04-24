namespace WoongBlog.Application.Modules.AI;

public interface IAiRuntimeCapabilities
{
    IReadOnlyList<string> GetAvailableProviders();
    string GetDefaultBlogFixPrompt();
    string GetDefaultWorkEnrichPrompt();
}
