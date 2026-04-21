namespace WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

public sealed record PageDto(Guid Id, string Slug, string Title, string ContentJson);
