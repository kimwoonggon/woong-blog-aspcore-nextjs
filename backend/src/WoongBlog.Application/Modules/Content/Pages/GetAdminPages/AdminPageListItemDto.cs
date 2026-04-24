namespace WoongBlog.Application.Modules.Content.Pages.GetAdminPages;

public sealed record AdminPageHtmlDto(string Html);

public sealed record AdminPageListItemDto(
    Guid Id,
    string Slug,
    string Title,
    object Content
);
