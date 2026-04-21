using MediatR;
using WoongBlog.Api.Modules.AI.Application;

namespace WoongBlog.Api.Modules.AI.Application.RuntimeConfig;

public sealed record GetAiRuntimeConfigQuery : IRequest<AiRuntimeConfigResponse>;
