using WoongBlog.Api.Modules.Content.Common.Application.Support;
using WoongBlog.Api.Modules.Content.Blogs.Application.CreateBlog;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogById;
using WoongBlog.Api.Modules.Content.Blogs.Application.GetAdminBlogs;
using WoongBlog.Api.Modules.Content.Blogs.Application.UpdateBlog;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.Abstractions;

public interface IAdminBlogService
{
    Task<IReadOnlyList<AdminBlogListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminBlogDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminMutationResult> CreateAsync(CreateBlogCommand command, CancellationToken cancellationToken);
    Task<AdminMutationResult?> UpdateAsync(UpdateBlogCommand command, CancellationToken cancellationToken);
    Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
