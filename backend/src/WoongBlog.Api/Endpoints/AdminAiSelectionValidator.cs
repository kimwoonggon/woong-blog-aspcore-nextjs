namespace WoongBlog.Api.Endpoints;

internal static class AdminAiSelectionValidator
{
    public static AdminAiSelectionValidationResult Validate(IReadOnlyList<Guid>? blogIds, bool all)
    {
        if (all)
        {
            return new AdminAiSelectionValidationResult(true, [], null);
        }

        if (blogIds is null || blogIds.Count == 0)
        {
            return new AdminAiSelectionValidationResult(false, [], "Either blogIds or all=true is required.");
        }

        var uniqueIds = blogIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (uniqueIds.Length == 0)
        {
            return new AdminAiSelectionValidationResult(false, [], "Either blogIds or all=true is required.");
        }

        return new AdminAiSelectionValidationResult(true, uniqueIds, null);
    }
}

internal sealed record AdminAiSelectionValidationResult(
    bool IsValid,
    IReadOnlyList<Guid> BlogIds,
    string? ErrorMessage);
