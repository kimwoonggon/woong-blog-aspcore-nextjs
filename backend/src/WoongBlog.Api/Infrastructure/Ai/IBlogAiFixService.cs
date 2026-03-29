namespace WoongBlog.Api.Infrastructure.Ai;

public interface IBlogAiFixService
{
    Task<BlogAiFixResult> FixHtmlAsync(string html, CancellationToken cancellationToken, AiFixRequestOptions? options = null);
}
