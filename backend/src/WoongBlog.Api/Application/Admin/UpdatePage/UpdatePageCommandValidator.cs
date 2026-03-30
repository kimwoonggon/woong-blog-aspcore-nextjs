using FluentValidation;
using WoongBlog.Api.Application.Validation;

namespace WoongBlog.Api.Application.Admin.UpdatePage;

public sealed class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentJson).NotEmpty().MustBeJsonObject();
    }
}
