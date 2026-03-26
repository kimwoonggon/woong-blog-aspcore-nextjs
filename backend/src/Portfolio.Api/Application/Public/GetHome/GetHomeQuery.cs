using MediatR;

namespace Portfolio.Api.Application.Public.GetHome;

public sealed record GetHomeQuery : IRequest<HomeDto?>;
