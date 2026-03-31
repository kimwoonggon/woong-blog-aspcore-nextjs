using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Works.Api.CreateWork;

public sealed class CreateWorkRequestValidator : AbstractValidator<CreateWorkRequest>
{
    public CreateWorkRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Period).MaximumLength(100);
        RuleFor(x => x.ContentJson).NotEmpty();
        RuleFor(x => x.AllPropertiesJson).NotEmpty();
        RuleForEach(x => x.Tags).MaximumLength(50);
    }
}
