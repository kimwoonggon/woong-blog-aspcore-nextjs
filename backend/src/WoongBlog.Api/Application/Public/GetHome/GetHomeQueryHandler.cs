using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetHome;

public class GetHomeQueryHandler : IRequestHandler<GetHomeQuery, HomeDto?>
{
    private readonly IPublicHomeQueries _publicHomeQueries;

    public GetHomeQueryHandler(IPublicHomeQueries publicHomeQueries)
    {
        _publicHomeQueries = publicHomeQueries;
    }

    public async Task<HomeDto?> Handle(GetHomeQuery request, CancellationToken cancellationToken)
    {
        return await _publicHomeQueries.GetHomeAsync(cancellationToken);
    }
}
