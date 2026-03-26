using Portfolio.Api.Application.Admin.CreateBlog;
using Portfolio.Api.Application.Admin.GetAdminBlogById;
using Portfolio.Api.Application.Admin.GetAdminBlogs;
using Portfolio.Api.Application.Admin.Support;
using Portfolio.Api.Application.Admin.UpdateBlog;

namespace Portfolio.Api.Application.Admin.Abstractions;

public interface IAdminBlogService
{
    Task<IReadOnlyList<AdminBlogListItemDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminBlogDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminMutationResult> CreateAsync(CreateBlogCommand command, CancellationToken cancellationToken);
    Task<AdminMutationResult?> UpdateAsync(UpdateBlogCommand command, CancellationToken cancellationToken);
    Task<AdminActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
