using MediatR;

namespace WoongBlog.Application.Modules.Composition.GetHome;

public sealed record GetHomeQuery : IRequest<HomeDto?>;
