using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Blogs.Application.GetBlogBySlug;

public class GetBlogBySlugQueryValidator : AbstractValidator<GetBlogBySlugQuery>
{
    public GetBlogBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120);
    }
}
