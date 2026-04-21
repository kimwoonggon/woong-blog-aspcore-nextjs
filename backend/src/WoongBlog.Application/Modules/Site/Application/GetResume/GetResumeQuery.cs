using MediatR;

namespace WoongBlog.Api.Modules.Site.Application.GetResume;

public sealed record GetResumeQuery : IRequest<ResumeDto?>;
