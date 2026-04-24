using MediatR;

namespace WoongBlog.Application.Modules.Site.GetResume;

public sealed record GetResumeQuery : IRequest<ResumeDto?>;
