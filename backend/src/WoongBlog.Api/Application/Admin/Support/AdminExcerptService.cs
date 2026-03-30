using WoongBlog.Api.Application.Admin.Abstractions;

namespace WoongBlog.Api.Application.Admin.Support;

public sealed class AdminExcerptService : IAdminExcerptService
{
    public string GenerateWorkExcerpt(string contentJson)
    {
        return AdminContentText.GenerateExcerpt(AdminContentJson.ExtractHtml(contentJson));
    }

    public string GenerateBlogExcerpt(string contentJson)
    {
        return AdminContentText.GenerateExcerpt(AdminContentJson.ExtractExcerptText(contentJson));
    }
}
