using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdateBlog;

public sealed class UpdateBlogCommandHandler : IRequestHandler<UpdateBlogCommand, AdminMutationResult?>
{
    private readonly IAdminBlogWriteStore _blogWriteStore;

    public UpdateBlogCommandHandler(IAdminBlogWriteStore blogWriteStore)
    {
        _blogWriteStore = blogWriteStore;
    }

    public async Task<AdminMutationResult?> Handle(UpdateBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await _blogWriteStore.FindByIdAsync(request.Id, cancellationToken);
        if (blog is null)
        {
            return null;
        }

        blog.Title = request.Title;
        blog.Slug = await GenerateUniqueSlugAsync(request.Title, blog.Id, cancellationToken);
        blog.Excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractExcerptText(request.ContentJson));
        blog.Tags = request.Tags;
        blog.ContentJson = request.ContentJson;
        blog.UpdatedAt = DateTimeOffset.UtcNow;
        blog.Published = request.Published;
        if (request.Published && blog.PublishedAt is null)
        {
            blog.PublishedAt = DateTimeOffset.UtcNow;
        }

        await _blogWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(blog.Id, blog.Slug);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken)
    {
        var baseSlug = AdminContentText.Slugify(title, "post");
        var slug = baseSlug;
        var suffix = 2;

        while (await _blogWriteStore.SlugExistsAsync(slug, excludingId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }
}
