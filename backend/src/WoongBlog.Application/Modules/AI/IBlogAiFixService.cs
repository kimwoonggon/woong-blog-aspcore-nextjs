namespace WoongBlog.Application.Modules.AI;

public interface IBlogAiFixService
{
    Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null);
}
