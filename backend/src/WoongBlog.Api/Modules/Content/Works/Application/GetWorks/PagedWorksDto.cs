using WoongBlog.Api.Modules.Composition.Application.GetHome;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetWorks;

public sealed record PagedWorksDto(
    IReadOnlyList<WorkCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
