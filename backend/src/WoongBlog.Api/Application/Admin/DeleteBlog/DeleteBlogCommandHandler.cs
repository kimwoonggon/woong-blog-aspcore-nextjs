using MediatR;
using WoongBlog.Api.Application.Admin.Abstractions;
using WoongBlog.Api.Application.Admin.Support;

namespace WoongBlog.Api.Application.Admin.DeleteBlog;

public sealed class DeleteBlogCommandHandler : IRequestHandler<DeleteBlogCommand, AdminActionResult>
{
    private readonly IAdminBlogWriteStore _blogWriteStore;

    public DeleteBlogCommandHandler(IAdminBlogWriteStore blogWriteStore)
    {
        _blogWriteStore = blogWriteStore;
    }

    public async Task<AdminActionResult> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await _blogWriteStore.FindByIdAsync(request.Id, cancellationToken);
        if (blog is null)
        {
            return new AdminActionResult(false);
        }

        _blogWriteStore.Remove(blog);
        await _blogWriteStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
