using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.UpdateBlog;

public sealed class UpdateBlogCommandHandler : IRequestHandler<UpdateBlogCommand, AdminMutationResult?>
{
    private readonly IAdminBlogWriteStore _blogWriteStore;
    private readonly IAdminUniqueSlugService _uniqueSlugService;
    private readonly IAdminExcerptService _excerptService;

    public UpdateBlogCommandHandler(
        IAdminBlogWriteStore blogWriteStore,
        IAdminUniqueSlugService uniqueSlugService,
        IAdminExcerptService excerptService)
    {
        _blogWriteStore = blogWriteStore;
        _uniqueSlugService = uniqueSlugService;
        _excerptService = excerptService;
    }

    public async Task<AdminMutationResult?> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await _blogWriteStore.FindByIdAsync(request.Id, cancellationToken);
        if (blog is null)
        {
            return null;
        }

        var slug = await _uniqueSlugService.GenerateBlogSlugAsync(request.Title, blog.Id, cancellationToken);
        var excerpt = _excerptService.GenerateBlogExcerpt(request.ContentJson);
        var values = new BlogUpsertValues(
            request.Title,
            request.Tags,
            request.Published,
            request.ContentJson);
        blog.Update(values, slug, excerpt, DateTimeOffset.UtcNow);

        await _blogWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(blog.Id, blog.Slug);
    }
}
