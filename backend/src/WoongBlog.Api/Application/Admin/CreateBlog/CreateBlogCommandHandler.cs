using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.CreateBlog;

public sealed class CreateBlogCommandHandler : IRequestHandler<CreateBlogCommand, AdminMutationResult>
{
    private readonly IAdminBlogWriteStore _blogWriteStore;
    private readonly IAdminUniqueSlugService _uniqueSlugService;
    private readonly IAdminExcerptService _excerptService;

    public CreateBlogCommandHandler(
        IAdminBlogWriteStore blogWriteStore,
        IAdminUniqueSlugService uniqueSlugService,
        IAdminExcerptService excerptService)
    {
        _blogWriteStore = blogWriteStore;
        _uniqueSlugService = uniqueSlugService;
        _excerptService = excerptService;
    }

    public async Task<AdminMutationResult> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        var slug = await _uniqueSlugService.GenerateBlogSlugAsync(request.Title, null, cancellationToken);
        var excerpt = _excerptService.GenerateBlogExcerpt(request.ContentJson);
        var now = DateTimeOffset.UtcNow;
        var values = new BlogUpsertValues(
            request.Title,
            request.Tags,
            request.Published,
            request.ContentJson);
        var blog = Blog.Create(values, slug, excerpt, now);

        _blogWriteStore.Add(blog);
        await _blogWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(blog.Id, blog.Slug);
    }
}
