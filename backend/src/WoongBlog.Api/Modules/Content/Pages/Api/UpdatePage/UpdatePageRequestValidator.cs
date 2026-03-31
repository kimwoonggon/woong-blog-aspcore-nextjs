using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Pages.Api.UpdatePage;

public sealed class UpdatePageRequestValidator : AbstractValidator<UpdatePageRequest>
{
    public UpdatePageRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentJson).NotEmpty();
    }
}
