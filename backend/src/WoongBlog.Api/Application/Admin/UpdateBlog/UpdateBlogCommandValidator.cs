using FluentValidation;
using WoongBlog.Api.Application.Validation;

namespace WoongBlog.Api.Application.Admin.UpdateBlog;

public sealed class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentJson).NotEmpty().MustBeJsonObject();
        RuleForEach(x => x.Tags).MaximumLength(50);
    }
}
