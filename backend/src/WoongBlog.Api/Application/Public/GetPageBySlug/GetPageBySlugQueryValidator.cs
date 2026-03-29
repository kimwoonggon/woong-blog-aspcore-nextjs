using FluentValidation;

namespace WoongBlog.Api.Application.Public.GetPageBySlug;

public class GetPageBySlugQueryValidator : AbstractValidator<GetPageBySlugQuery>
{
    public GetPageBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100);
    }
}
