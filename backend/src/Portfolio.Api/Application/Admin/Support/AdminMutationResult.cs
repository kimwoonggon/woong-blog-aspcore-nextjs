namespace Portfolio.Api.Application.Admin.Support;

public sealed record AdminMutationResult(
    Guid Id,
    string Slug
);
