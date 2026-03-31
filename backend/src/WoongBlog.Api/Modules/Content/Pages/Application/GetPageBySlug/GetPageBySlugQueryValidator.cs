using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Pages.Application.GetPageBySlug;

public class GetPageBySlugQueryValidator : AbstractValidator<GetPageBySlugQuery>
{
    public GetPageBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100);
    }
}
