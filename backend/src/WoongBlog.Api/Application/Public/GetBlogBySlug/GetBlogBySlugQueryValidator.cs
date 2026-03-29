using FluentValidation;

namespace WoongBlog.Api.Application.Public.GetBlogBySlug;

public class GetBlogBySlugQueryValidator : AbstractValidator<GetBlogBySlugQuery>
{
    public GetBlogBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120);
    }
}
