namespace WoongBlog.Api.Modules.Content.Common.Application.Support;

public sealed record AdminMutationResult(
    Guid Id,
    string Slug
);
