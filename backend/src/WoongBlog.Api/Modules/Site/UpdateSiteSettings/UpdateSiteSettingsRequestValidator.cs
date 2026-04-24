using FluentValidation;

namespace WoongBlog.Api.Modules.Site.UpdateSiteSettings;

public sealed class UpdateSiteSettingsRequestValidator : AbstractValidator<UpdateSiteSettingsRequest>
{
    public UpdateSiteSettingsRequestValidator()
    {
        RuleFor(x => x.OwnerName).MaximumLength(200);
        RuleFor(x => x.Tagline).MaximumLength(200);
        RuleFor(x => x.FacebookUrl).MaximumLength(400);
        RuleFor(x => x.InstagramUrl).MaximumLength(400);
        RuleFor(x => x.TwitterUrl).MaximumLength(400);
        RuleFor(x => x.LinkedInUrl).MaximumLength(400);
        RuleFor(x => x.GitHubUrl).MaximumLength(400);
        RuleFor(x => x.ResumeAssetId)
            .NotEqual(Guid.Empty)
            .When(x => x.HasResumeAssetId && x.ResumeAssetId.HasValue);
    }
}
