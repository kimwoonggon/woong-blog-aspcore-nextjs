using MediatR;

namespace WoongBlog.Api.Modules.Composition.Application.GetHome;

public sealed record GetHomeQuery : IRequest<HomeDto?>;
