using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.CreateWork;

public sealed class CreateWorkCommandHandler : IRequestHandler<CreateWorkCommand, AdminMutationResult>
{
    private readonly IAdminWorkWriteStore _workWriteStore;

    public CreateWorkCommandHandler(IAdminWorkWriteStore workWriteStore)
    {
        _workWriteStore = workWriteStore;
    }

    public async Task<AdminMutationResult> Handle(CreateWorkCommand request, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(request.Title, null, cancellationToken);
        var excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(request.ContentJson));
        var now = DateTimeOffset.UtcNow;

        var work = new Work
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = slug,
            Excerpt = excerpt,
            ThumbnailAssetId = request.ThumbnailAssetId,
            IconAssetId = request.IconAssetId,
            Category = request.Category,
            Period = request.Period,
            Tags = request.Tags,
            Published = request.Published,
            PublishedAt = request.Published ? now : null,
            ContentJson = request.ContentJson,
            AllPropertiesJson = request.AllPropertiesJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        _workWriteStore.Add(work);
        await _workWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(work.Id, work.Slug);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken)
    {
        var baseSlug = AdminContentText.Slugify(title, "work");
        var slug = baseSlug;
        var suffix = 2;

        while (await _workWriteStore.SlugExistsAsync(slug, excludingId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }
}
