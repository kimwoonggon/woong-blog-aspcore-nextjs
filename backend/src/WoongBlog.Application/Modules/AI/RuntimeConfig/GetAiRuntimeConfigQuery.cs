using MediatR;
using WoongBlog.Application.Modules.AI;

namespace WoongBlog.Application.Modules.AI.RuntimeConfig;

public sealed record GetAiRuntimeConfigQuery : IRequest<AiRuntimeConfigResponse>;
