namespace WoongBlog.Application.Modules.Content.Common.Support;

public sealed record AdminMutationResult(
    Guid Id,
    string Slug
);
