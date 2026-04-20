using MediatR;
using WoongBlog.Api.Modules.AI.Api;

namespace WoongBlog.Api.Modules.AI.Application.WorkEnrich;

public sealed record EnrichWorkHtmlCommand(
    string Html,
    string? Title,
    string? Provider,
    string? CodexModel,
    string? CodexReasoningEffort,
    string? CustomPrompt) : IRequest<AiActionResult<WorkEnrichResponse>>;
