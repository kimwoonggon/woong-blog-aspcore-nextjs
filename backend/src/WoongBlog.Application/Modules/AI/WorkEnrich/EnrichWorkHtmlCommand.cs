using MediatR;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Application.Modules.AI.WorkEnrich;

public sealed record EnrichWorkHtmlCommand(
    string Html,
    string? Title,
    string? Provider,
    string? CodexModel,
    string? CodexReasoningEffort,
    string? CustomPrompt) : IRequest<AiActionResult<WorkEnrichResponse>>;
