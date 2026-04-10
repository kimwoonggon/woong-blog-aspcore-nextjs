using FluentValidation;

namespace WoongBlog.Api.Modules.Content.Works.Api.WorkVideos;

public sealed class IssueWorkVideoUploadRequest
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public int ExpectedVideosVersion { get; init; }
}

public sealed class IssueWorkVideoUploadRequestValidator : AbstractValidator<IssueWorkVideoUploadRequest>
{
    public IssueWorkVideoUploadRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Size).GreaterThan(0);
        RuleFor(x => x.ExpectedVideosVersion).GreaterThanOrEqualTo(0);
    }
}
