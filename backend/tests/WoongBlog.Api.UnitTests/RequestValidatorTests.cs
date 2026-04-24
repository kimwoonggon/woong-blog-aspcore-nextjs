using WoongBlog.Application.Modules.Content.Blogs.CreateBlog;
using WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;
using WoongBlog.Application.Modules.Content.Pages.UpdatePage;
using WoongBlog.Application.Modules.Content.Works.CreateWork;
using WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;
using WoongBlog.Application.Modules.Site.UpdateSiteSettings;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Unit)]
public class RequestValidatorTests
{
    [Fact]
    public void SaveWorkRequestValidator_Rejects_Empty_Title_Category_And_Content()
    {
        var validator = new CreateWorkCommandValidator();
        var result = validator.Validate(new CreateWorkCommand(
            string.Empty,
            string.Empty,
            string.Empty,
            [new string('x', 51)],
            false,
            string.Empty,
            "{}",
            null,
            null
        ));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateWorkCommand.Title));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateWorkCommand.Category));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateWorkCommand.ContentJson));
        Assert.Contains(result.Errors, error => error.PropertyName == "Tags[0]");
    }

    [Fact]
    public void SaveBlogRequestValidator_Rejects_TooLong_Title_And_Tag()
    {
        var validator = new CreateBlogCommandValidator();
        var result = validator.Validate(new CreateBlogCommand(
            new string('t', 201),
            null,
            [new string('x', 51)],
            false,
            "{\"html\":\"ok\"}"
        ));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBlogCommand.Title));
        Assert.Contains(result.Errors, error => error.PropertyName == "Tags[0]");
    }

    [Fact]
    public void UpdatePageRequestValidator_Rejects_Empty_Id_TooLong_Title_And_Empty_Content()
    {
        var validator = new UpdatePageCommandValidator();
        var result = validator.Validate(new UpdatePageCommand(
            Guid.Empty,
            new string('p', 201),
            string.Empty
        ));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdatePageCommand.Id));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdatePageCommand.Title));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdatePageCommand.ContentJson));
    }

    [Fact]
    public void UpdateSiteSettingsCommandValidator_Rejects_Empty_ResumeAssetId_WhenProvided()
    {
        var validator = new UpdateSiteSettingsCommandValidator();
        var result = validator.Validate(new UpdateSiteSettingsCommand(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            Guid.Empty,
            true
        ));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateSiteSettingsCommand.ResumeAssetId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GetWorkBySlugQueryValidator_Rejects_Empty_Slug(string slug)
    {
        var validator = new GetWorkBySlugQueryValidator();
        var result = validator.Validate(new GetWorkBySlugQuery(slug));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetWorkBySlugQuery.Slug));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GetBlogBySlugQueryValidator_Rejects_Empty_Slug(string slug)
    {
        var validator = new GetBlogBySlugQueryValidator();
        var result = validator.Validate(new GetBlogBySlugQuery(slug));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetBlogBySlugQuery.Slug));
    }
}
