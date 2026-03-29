using MediatR;

namespace WoongBlog.Api.Application.Public.GetHome;

public sealed record GetHomeQuery : IRequest<HomeDto?>;
