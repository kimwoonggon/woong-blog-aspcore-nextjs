using MediatR;

namespace WoongBlog.Api.Application.Public.GetResume;

public sealed record GetResumeQuery : IRequest<ResumeDto?>;
