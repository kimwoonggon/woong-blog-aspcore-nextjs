using Portfolio.Api.Application.Public.GetHome;

namespace Portfolio.Api.Application.Public.GetWorks;

public sealed record PagedWorksDto(
    IReadOnlyList<WorkCardDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
