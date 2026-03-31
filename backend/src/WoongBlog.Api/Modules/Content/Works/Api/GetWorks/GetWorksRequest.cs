using WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

namespace WoongBlog.Api.Modules.Content.Works.Api.GetWorks;

internal sealed class GetWorksRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }

    internal GetWorksQuery ToQuery() => new(Page ?? 1, PageSize ?? 6);
}
