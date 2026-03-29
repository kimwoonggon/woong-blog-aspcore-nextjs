using FluentValidation;

namespace WoongBlog.Api.Application.Admin.CreateBlog;

public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentJson).NotEmpty();
        RuleForEach(x => x.Tags).MaximumLength(50);
    }
}
