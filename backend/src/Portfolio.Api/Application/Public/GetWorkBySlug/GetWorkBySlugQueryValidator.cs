using FluentValidation;

namespace Portfolio.Api.Application.Public.GetWorkBySlug;

public class GetWorkBySlugQueryValidator : AbstractValidator<GetWorkBySlugQuery>
{
    public GetWorkBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120);
    }
}
