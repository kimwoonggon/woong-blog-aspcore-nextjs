namespace Portfolio.Api.Application.Public.GetPageBySlug;

public sealed record PageDto(Guid Id, string Slug, string Title, string ContentJson);
