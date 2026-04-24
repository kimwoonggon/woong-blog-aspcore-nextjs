namespace WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;

public sealed record PageDto(Guid Id, string Slug, string Title, string ContentJson);
