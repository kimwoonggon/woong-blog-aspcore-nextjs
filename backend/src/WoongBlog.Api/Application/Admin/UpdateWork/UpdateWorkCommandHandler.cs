using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.UpdateWork;

public sealed class UpdateWorkCommandHandler : IRequestHandler<UpdateWorkCommand, AdminMutationResult?>
{
    private readonly IAdminWorkWriteStore _workWriteStore;

    public UpdateWorkCommandHandler(IAdminWorkWriteStore workWriteStore)
    {
        _workWriteStore = workWriteStore;
    }

    public async Task<AdminMutationResult?> Handle(UpdateWorkCommand request, CancellationToken cancellationToken)
    {
        var work = await _workWriteStore.FindByIdAsync(request.Id, cancellationToken);
        if (work is null)
        {
            return null;
        }

        work.Title = request.Title;
        work.Slug = await GenerateUniqueSlugAsync(request.Title, work.Id, cancellationToken);
        work.Excerpt = AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(request.ContentJson));
        work.ThumbnailAssetId = request.ThumbnailAssetId;
        work.IconAssetId = request.IconAssetId;
        work.Category = request.Category;
        work.Period = request.Period;
        work.Tags = request.Tags;
        work.ContentJson = request.ContentJson;
        work.AllPropertiesJson = request.AllPropertiesJson;
        work.UpdatedAt = DateTimeOffset.UtcNow;
        work.Published = request.Published;
        if (request.Published && work.PublishedAt is null)
        {
            work.PublishedAt = DateTimeOffset.UtcNow;
        }

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
