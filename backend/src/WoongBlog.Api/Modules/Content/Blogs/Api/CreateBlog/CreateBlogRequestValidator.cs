using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Blogs.Api.CreateBlog;

public sealed class CreateBlogRequestValidator : AbstractValidator<CreateBlogRequest>
{
    public CreateBlogRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Excerpt).MaximumLength(200);
        RuleFor(x => x.ContentJson).NotEmpty();
        RuleForEach(x => x.Tags).MaximumLength(50);
    }
}
