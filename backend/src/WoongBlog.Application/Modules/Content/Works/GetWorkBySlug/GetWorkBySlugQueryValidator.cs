using FluentValidation;

namespace WoongBlog.Application.Modules.Content.Works.GetWorkBySlug;

public class GetWorkBySlugQueryValidator : AbstractValidator<GetWorkBySlugQuery>
{
    public GetWorkBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120);
    }
}
