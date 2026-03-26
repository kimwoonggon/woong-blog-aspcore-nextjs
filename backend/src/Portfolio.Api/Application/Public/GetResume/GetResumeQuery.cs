using MediatR;

namespace Portfolio.Api.Application.Public.GetResume;

public sealed record GetResumeQuery : IRequest<ResumeDto?>;
