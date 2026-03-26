using Portfolio.Api.Application.Public.GetPageBySlug;

namespace Portfolio.Api.Tests;

public class GetPageBySlugQueryValidatorTests
{
    private readonly GetPageBySlugQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Slug_Is_Empty()
    {
        var result = _validator.Validate(new GetPageBySlugQuery(string.Empty));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetPageBySlugQuery.Slug));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Slug_Is_Valid()
    {
        var result = _validator.Validate(new GetPageBySlugQuery("seeded-work"));

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(GetPageBySlugQuery.Slug));
    }
}
