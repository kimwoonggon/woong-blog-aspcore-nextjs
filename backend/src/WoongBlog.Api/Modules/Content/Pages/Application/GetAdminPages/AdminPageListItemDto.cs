namespace WoongBlog.Api.Modules.Content.Pages.Application.GetAdminPages;

public sealed record AdminPageHtmlDto(string Html);

public sealed record AdminPageListItemDto(
    Guid Id,
    string Slug,
    string Title,
    AdminPageHtmlDto Content
);
