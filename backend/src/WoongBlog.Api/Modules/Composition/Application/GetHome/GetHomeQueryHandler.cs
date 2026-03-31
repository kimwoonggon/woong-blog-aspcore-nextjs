using MediatR;
using WoongBlog.Api.Modules.Composition.Application.Abstractions;

namespace WoongBlog.Api.Modules.Composition.Application.GetHome;

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
