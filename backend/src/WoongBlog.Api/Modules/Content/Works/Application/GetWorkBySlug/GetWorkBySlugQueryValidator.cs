using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Works.Application.GetWorkBySlug;

public class GetWorkBySlugQueryValidator : AbstractValidator<GetWorkBySlugQuery>
{
    public GetWorkBySlugQueryValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(120);
    }
}
