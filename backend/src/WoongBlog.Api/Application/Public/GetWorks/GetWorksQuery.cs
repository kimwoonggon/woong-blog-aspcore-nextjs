using MediatR;

namespace WoongBlog.Api.Application.Public.GetWorks;

public sealed record GetWorksQuery(int Page = 1, int PageSize = 6) : IRequest<PagedWorksDto>;
