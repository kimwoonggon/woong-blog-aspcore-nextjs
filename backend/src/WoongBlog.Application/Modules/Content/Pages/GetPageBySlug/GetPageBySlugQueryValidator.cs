using FluentValidation;

namespace WoongBlog.Application.Modules.Content.Pages.GetPageBySlug;

public class GetPageBySlugQueryValidator : AbstractValidator<GetPageBySlugQuery>
{
    public GetPageBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100);
    }
}
