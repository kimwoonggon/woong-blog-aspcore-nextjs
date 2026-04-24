using FluentValidation;

namespace WoongBlog.Application.Modules.Content.Blogs.GetBlogBySlug;

public class GetBlogBySlugQueryValidator : AbstractValidator<GetBlogBySlugQuery>
{
    public GetBlogBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120);
    }
}
