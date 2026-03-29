namespace WoongBlog.Api.Application.Admin.GetAdminPages;

public sealed record AdminPageHtmlDto(string Html);

public sealed record AdminPageListItemDto(
    Guid Id,
    string Slug,
    string Title,
    AdminPageHtmlDto Content
);
