using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Works.Api.WorkVideos;

public sealed class ConfirmWorkVideoUploadRequest
{
    public Guid UploadSessionId { get; init; }
    public int ExpectedVideosVersion { get; init; }
}

public sealed class ConfirmWorkVideoUploadRequestValidator : AbstractValidator<ConfirmWorkVideoUploadRequest>
{
    public ConfirmWorkVideoUploadRequestValidator()
    {
        RuleFor(x => x.UploadSessionId).NotEmpty();
        RuleFor(x => x.ExpectedVideosVersion).GreaterThanOrEqualTo(0);
    }
}
