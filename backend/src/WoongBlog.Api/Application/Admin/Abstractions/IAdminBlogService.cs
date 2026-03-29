using WoongBlog.Api.Application.Admin.CreateBlog;
using WoongBlog.Api.Application.Admin.GetAdminBlogById;
using WoongBlog.Api.Application.Admin.GetAdminBlogs;
using WoongBlog.Api.Application.Admin.Support;
using WoongBlog.Api.Application.Admin.UpdateBlog;

namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminBlogService
{
    Task<IReadOnlyList<AdminBlogListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminBlogDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminMutationResult> CreateAsync(CreateBlogCommand command, CancellationToken cancellationToken);
    Task<AdminMutationResult?> UpdateAsync(UpdateBlogCommand command, CancellationToken cancellationToken);
    Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
