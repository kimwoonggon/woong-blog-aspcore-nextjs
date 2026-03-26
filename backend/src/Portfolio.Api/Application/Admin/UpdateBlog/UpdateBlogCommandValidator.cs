using FluentValidation;

namespace Portfolio.Api.Application.Admin.UpdateBlog;

public sealed class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContentJson).NotEmpty();
        RuleForEach(x => x.Tags).MaximumLength(50);
    }
}
