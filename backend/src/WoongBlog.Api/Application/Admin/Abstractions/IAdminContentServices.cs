namespace WoongBlog.Api.Application.Admin.Abstractions;

public interface IAdminUniqueSlugService
{
    Task<string> GenerateWorkSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken);
    Task<string> GenerateBlogSlugAsync(string title, Guid? excludingId, CancellationToken cancellationToken);
}

public interface IAdminExcerptService
{
    string GenerateWorkExcerpt(string contentJson);
    string GenerateBlogExcerpt(string contentJson);
}
