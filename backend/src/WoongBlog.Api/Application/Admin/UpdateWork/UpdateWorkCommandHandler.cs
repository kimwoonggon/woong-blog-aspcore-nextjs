using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Domain.Entities;

namespace WoongBlog.Api.Application.Admin.UpdateWork;

public sealed class UpdateWorkCommandHandler : IRequestHandler<UpdateWorkCommand, AdminMutationResult?>
{
    private readonly IAdminWorkWriteStore _workWriteStore;
    private readonly IAdminUniqueSlugService _uniqueSlugService;
    private readonly IAdminExcerptService _excerptService;

    public UpdateWorkCommandHandler(
        IAdminWorkWriteStore workWriteStore,
        IAdminUniqueSlugService uniqueSlugService,
        IAdminExcerptService excerptService)
    {
        _workWriteStore = workWriteStore;
        _uniqueSlugService = uniqueSlugService;
        _excerptService = excerptService;
    }

    public async Task<AdminMutationResult?> Handle(UpdateWorkCommand request, CancellationToken cancellationToken)
    {
        var work = await _workWriteStore.FindByIdAsync(request.Id, cancellationToken);
        if (work is null)
        {
            return null;
        }

        var slug = await _uniqueSlugService.GenerateWorkSlugAsync(request.Title, work.Id, cancellationToken);
        var excerpt = _excerptService.GenerateWorkExcerpt(request.ContentJson);
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
        work.Update(values, slug, excerpt, DateTimeOffset.UtcNow);

        await _workWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminMutationResult(work.Id, work.Slug);
    }
}
