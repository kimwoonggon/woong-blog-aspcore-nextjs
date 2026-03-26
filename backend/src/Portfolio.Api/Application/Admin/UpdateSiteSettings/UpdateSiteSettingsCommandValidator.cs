using FluentValidation;

namespace Portfolio.Api.Application.Admin.UpdateSiteSettings;

public sealed class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    public UpdateSiteSettingsCommandValidator()
    {
        RuleFor(x => x.ResumeAssetId)
            .NotEqual(Guid.Empty)
            .When(x => x.HasResumeAssetId && x.ResumeAssetId.HasValue);
    }
}
