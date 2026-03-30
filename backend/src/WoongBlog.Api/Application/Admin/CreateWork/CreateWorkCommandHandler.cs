using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.CreateWork;

public sealed class CreateWorkCommandHandler : IRequestHandler<CreateWorkCommand, AdminMutationResult>
{
    private readonly IAdminWorkWriteStore _workWriteStore;
    private readonly IAdminUniqueSlugService _uniqueSlugService;
    private readonly IAdminExcerptService _excerptService;

    public CreateWorkCommandHandler(
        IAdminWorkWriteStore workWriteStore,
        IAdminUniqueSlugService uniqueSlugService,
        IAdminExcerptService excerptService)
    {
        _workWriteStore = workWriteStore;
        _uniqueSlugService = uniqueSlugService;
        _excerptService = excerptService;
    }

    public async Task<AdminMutationResult> Handle(CreateWorkCommand request, CancellationToken cancellationToken)
    {
        var slug = await _uniqueSlugService.GenerateWorkSlugAsync(request.Title, null, cancellationToken);
        var excerpt = _excerptService.GenerateWorkExcerpt(request.ContentJson);
        var now = DateTimeOffset.UtcNow;
        var values = new WorkUpsertValues(
            request.Title,
            request.Category,
            request.Period,
            request.Tags,
            request.Published,
            request.ContentJson,
            request.AllPropertiesJson,
            request.ThumbnailAssetId,
            request.IconAssetId);
        var work = Work.Create(values, slug, excerpt, now);

        _workWriteStore.Add(work);
        await _workWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(work.Id, work.Slug);
    }
}
