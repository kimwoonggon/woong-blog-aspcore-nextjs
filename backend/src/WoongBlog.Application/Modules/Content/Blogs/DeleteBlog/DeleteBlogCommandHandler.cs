using MediatR;
using WoongBlog.Application.Modules.Content.Common.Support;
using WoongBlog.Application.Modules.Content.Blogs.Abstractions;

namespace WoongBlog.Application.Modules.Content.Blogs.DeleteBlog;

public sealed class DeleteBlogCommandHandler : IRequestHandler<DeleteBlogCommand, AdminActionResult>
{
    private readonly IBlogCommandStore _blogCommandStore;

    public DeleteBlogCommandHandler(IBlogCommandStore blogCommandStore)
    {
        _blogCommandStore = blogCommandStore;
    }

    public async Task<AdminActionResult> Handle(DeleteBlogCommand request, CancellationToken cancellationToken)
    {
        var blog = await _blogCommandStore.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (blog is null)
        {
            return new AdminActionResult(false);
        }

        _blogCommandStore.Remove(blog);
        await _blogCommandStore.SaveChangesAsync(cancellationToken);
        return new AdminActionResult(true);
    }
}
