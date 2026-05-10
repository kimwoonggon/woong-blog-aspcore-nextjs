using WoongBlog.Application.Modules.Content.Common;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Unit)]
public class PublicContentBodyDtoTests
{
    [Fact]
    public void FromStoredFields_ReturnsOnlyMarkdown_WhenBothStoredSourcesExist()
    {
        var content = PublicContentBodyDto.FromStoredFields("<p>stored html</p>", "# stored markdown");

        Assert.Null(content.Html);
        Assert.Equal("# stored markdown", content.Markdown);
    }

    [Fact]
    public void FromStoredFields_ReturnsHtml_WhenMarkdownIsMissing()
    {
        var content = PublicContentBodyDto.FromStoredFields("<p>stored html</p>", string.Empty);

        Assert.Equal("<p>stored html</p>", content.Html);
        Assert.Null(content.Markdown);
    }
}
