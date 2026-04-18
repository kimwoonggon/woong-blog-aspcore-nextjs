using WoongBlog.Api.Modules.Content.Common.Application.Support;

namespace WoongBlog.Api.Tests;

public class AdminContentTextTests
{
    [Fact]
    public void GenerateExcerpt_Removes_MermaidBlocks_And_Preserves_AdjacentText()
    {
        var excerpt = AdminContentText.GenerateExcerpt(
            """
            <p>Before</p>
            <mermaid-block data-code="sequenceDiagram
            User->>Frontend: Login
            A --> B"></mermaid-block>
            <p>After</p>
            """);

        Assert.Contains("Before", excerpt);
        Assert.Contains("After", excerpt);
        Assert.DoesNotContain("sequenceDiagram", excerpt);
        Assert.DoesNotContain("User->>Frontend", excerpt);
        Assert.DoesNotContain("A --> B", excerpt);
    }

    [Fact]
    public void GenerateExcerpt_DoesNotTreatPlainFencesAsManagedMermaidBlocks()
    {
        var excerpt = AdminContentText.GenerateExcerpt(
            """
            Intro
            ```mermaid
            sequenceDiagram
            User->>Frontend: Login
            ```
            Outro
            """);

        Assert.Contains("Intro", excerpt);
        Assert.Contains("Outro", excerpt);
        Assert.Contains("sequenceDiagram", excerpt);
        Assert.Contains("User->>Frontend", excerpt);
    }
}
