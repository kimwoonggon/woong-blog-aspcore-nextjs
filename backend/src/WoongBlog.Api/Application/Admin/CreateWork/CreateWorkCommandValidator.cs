using FluentValidation;
using WoongBlog.Api.Application.Validation;

namespace WoongBlog.Api.Application.Admin.CreateWork;

public sealed class CreateWorkCommandValidator : AbstractValidator<CreateWorkCommand>
{
    public CreateWorkCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContentJson).NotEmpty().MustBeJsonObject();
        RuleFor(x => x.AllPropertiesJson).NotEmpty().MustBeJsonObject();
        RuleForEach(x => x.Tags).MaximumLength(50);
        RuleFor(x => x.ThumbnailAssetId)
            .NotEqual(Guid.Empty)
            .When(x => x.ThumbnailAssetId.HasValue);
        RuleFor(x => x.IconAssetId)
            .NotEqual(Guid.Empty)
            .When(x => x.IconAssetId.HasValue);
    }
}
