using WoongBlog.Application.Modules.Content.Works.GetWorks;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkDetailContext;

public sealed record WorkDetailContextDto(
    WorkCardDto? Newer,
    WorkCardDto? Older,
    IReadOnlyList<WorkCardDto> Related);
