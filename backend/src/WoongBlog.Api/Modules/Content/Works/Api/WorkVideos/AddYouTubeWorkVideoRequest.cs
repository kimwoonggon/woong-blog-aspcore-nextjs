using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Works.Api.WorkVideos;

public sealed class AddYouTubeWorkVideoRequest
{
    public string YoutubeUrlOrId { get; init; } = string.Empty;
    public int ExpectedVideosVersion { get; init; }
}

public sealed class AddYouTubeWorkVideoRequestValidator : AbstractValidator<AddYouTubeWorkVideoRequest>
{
    public AddYouTubeWorkVideoRequestValidator()
    {
        RuleFor(x => x.YoutubeUrlOrId).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpectedVideosVersion).GreaterThanOrEqualTo(0);
    }
}
