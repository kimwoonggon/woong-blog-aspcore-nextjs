using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Works.Application.UpdateWork;

public sealed class UpdateWorkCommandValidator : AbstractValidator<UpdateWorkCommand>
{
    public UpdateWorkCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContentJson).NotEmpty();
        RuleForEach(x => x.Tags).MaximumLength(50);
        RuleFor(x => x.ThumbnailAssetId)
            .NotEqual(Guid.Empty)
            .When(x => x.ThumbnailAssetId.HasValue);
        RuleFor(x => x.IconAssetId)
            .NotEqual(Guid.Empty)
            .When(x => x.IconAssetId.HasValue);
    }
}
