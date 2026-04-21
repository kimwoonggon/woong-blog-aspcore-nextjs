using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Pages.Application.UpdatePage;

public sealed class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentJson).NotEmpty();
    }
}
