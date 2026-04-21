namespace WoongBlog.Api.Modules.AI.Application;

public interface IBlogAiFixService
{
    Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null);
}
