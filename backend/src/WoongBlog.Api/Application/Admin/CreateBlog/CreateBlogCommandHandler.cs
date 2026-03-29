using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.CreateBlog;

public sealed class CreateBlogCommandHandler : IRequestHandler<CreateBlogCommand, AdminMutationResult>
{
    private readonly IAdminBlogWriteStore _blogWriteStore;

    public CreateBlogCommandHandler(IAdminBlogWriteStore blogWriteStore)
    {
        _blogWriteStore = blogWriteStore;
    }

    public async Task<AdminMutationResult> Handle(CreateBlogCommand request, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(request.Title, null, cancellationToken);
        var excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractExcerptText(request.ContentJson));
        var now = DateTimeOffset.UtcNow;

        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = slug,
            Excerpt = excerpt,
            Tags = request.Tags,
            Published = request.Published,
            PublishedAt = request.Published ? now : null,
            ContentJson = request.ContentJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        _blogWriteStore.Add(blog);
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
