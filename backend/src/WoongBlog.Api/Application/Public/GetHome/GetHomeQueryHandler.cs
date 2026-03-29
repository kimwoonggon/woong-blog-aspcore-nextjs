using MediatR;
using WoongBlog.Api.Application.Public.Abstractions;

namespace WoongBlog.Api.Application.Public.GetHome;

public class GetHomeQueryHandler : IRequestHandler<GetHomeQuery, HomeDto?>
{
    private readonly IPublicHomeService _publicHomeService;

    public GetHomeQueryHandler(IPublicHomeService publicHomeService)
    {
        _publicHomeService = publicHomeService;
    }

    public async Task<HomeDto?> Handle(GetHomeQuery request, CancellationToken cancellationToken)
    {
        return await _publicHomeService.GetHomeAsync(cancellationToken);
    }
}
