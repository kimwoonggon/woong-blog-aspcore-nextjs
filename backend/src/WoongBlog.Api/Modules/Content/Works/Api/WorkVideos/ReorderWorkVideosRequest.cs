using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Works.Api.WorkVideos;

public sealed class ReorderWorkVideosRequest
{
    public Guid[] OrderedVideoIds { get; init; } = [];
    public int ExpectedVideosVersion { get; init; }
}

public sealed class ReorderWorkVideosRequestValidator : AbstractValidator<ReorderWorkVideosRequest>
{
    public ReorderWorkVideosRequestValidator()
    {
        RuleFor(x => x.OrderedVideoIds).NotNull();
        RuleForEach(x => x.OrderedVideoIds).NotEmpty();
        RuleFor(x => x.ExpectedVideosVersion).GreaterThanOrEqualTo(0);
    }
}
