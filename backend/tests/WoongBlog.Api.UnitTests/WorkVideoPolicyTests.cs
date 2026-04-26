using WoongBlog.Application.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Unit)]
public sealed class WorkVideoPolicyTests
{
    private const string YouTubeVideoId = "abcDEF123_-";

    public static TheoryData<string, string> ValidYouTubeVideoIdInputs => new()
    {
        { YouTubeVideoId, YouTubeVideoId },
        { $"https://youtu.be/{YouTubeVideoId}", YouTubeVideoId },
        { $"https://youtu.be/{YouTubeVideoId}?si=share-token", YouTubeVideoId },
        { $"https://www.youtube.com/watch?v={YouTubeVideoId}", YouTubeVideoId },
        { $"https://youtube.com/watch?v={YouTubeVideoId}", YouTubeVideoId },
        { $"https://m.youtube.com/watch?v={YouTubeVideoId}", YouTubeVideoId },
        { $"https://www.youtube.com/watch?x=1&v={YouTubeVideoId}", YouTubeVideoId },
        { $"https://www.youtube.com/embed/{YouTubeVideoId}", YouTubeVideoId },
        { $"https://www.youtube.com/shorts/{YouTubeVideoId}", YouTubeVideoId },
    };

    public static TheoryData<string> InvalidYouTubeVideoIdInputs => new()
    {
        "   ",
        $"youtube.com/watch?v={YouTubeVideoId}",
        $"https://vimeo.com/{YouTubeVideoId}",
        "https://www.youtube.com/watch?x=1",
        "https://www.youtube.com/watch?v=",
        "abcDEF123_",
        "abcDEF123_-x",
        "abcDEF123!*",
        $"https://www.youtube.com/embed/{YouTubeVideoId}/extra",
        "https://www.youtube.com/embed",
        $"https://www.youtube.com/shorts/{YouTubeVideoId}/extra",
        "https://www.youtube.com/shorts",
        "https://youtu.be",
    };

    public static TheoryData<string, string, long> InvalidVideoFiles => new()
    {
        { "video.mp4", "video/mp4", 0 },
        { "video.mp4", "video/mp4", WorkVideoPolicy.MaxVideoBytes + 1 },
        { "video.mov", "video/mp4", 1024 },
        { "video.mp4", "video/quicktime", 1024 },
    };

    public static TheoryData<byte[], bool> Mp4Prefixes => new()
    {
        { new byte[] { 0, 0, 0, 0, (byte)'f', (byte)'t', (byte)'y', (byte)'p', 0, 0, 0 }, false },
        { new byte[] { 0, 0, 0, 0, (byte)'f', (byte)'t', (byte)'y', (byte)'p', 0, 0, 0, 0 }, true },
        { new byte[] { 0, 0, 0, 0, (byte)'m', (byte)'o', (byte)'o', (byte)'v', 0, 0, 0, 0 }, false },
        { new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, (byte)'f', (byte)'t', (byte)'y', (byte)'p', 0, 0, 0, 0 }, true },
    };

    public static TheoryData<string, string> OriginalFileNames => new()
    {
        { "video.mp4", "video.mp4" },
        { "../uploads/secret.mp4", "secret.mp4" },
        { "  clip.mp4  ", "clip.mp4" },
        { $"{new string('v', 130)}.mp4", new string('v', 120) },
    };

    [Theory]
    [MemberData(nameof(ValidYouTubeVideoIdInputs))]
    public void NormalizeYouTubeVideoId_ReturnsId_ForSupportedInput(string input, string expected)
    {
        var result = WorkVideoPolicy.NormalizeYouTubeVideoId(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(InvalidYouTubeVideoIdInputs))]
    public void NormalizeYouTubeVideoId_ReturnsNull_ForUnsupportedInput(string input)
    {
        var result = WorkVideoPolicy.NormalizeYouTubeVideoId(input);

        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(InvalidVideoFiles))]
    public void ValidateVideoFile_ReturnsError_ForInvalidUpload(string fileName, string contentType, long size)
    {
        var result = WorkVideoPolicy.ValidateVideoFile(fileName, contentType, size);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("video.MP4", "video/mp4", 1024)]
    [InlineData("video.mp4", "video/mp4", 1024)]
    public void ValidateVideoFile_ReturnsNull_ForSupportedUpload(string fileName, string contentType, long size)
    {
        var result = WorkVideoPolicy.ValidateVideoFile(fileName, contentType, size);

        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(Mp4Prefixes))]
    public void LooksLikeMp4_ReturnsExpectedResult_ForPrefix(byte[] prefix, bool expected)
    {
        var result = WorkVideoPolicy.LooksLikeMp4(prefix);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(OriginalFileNames))]
    public void SanitizeOriginalFileName_ReturnsExpectedName(string input, string expected)
    {
        var result = WorkVideoPolicy.SanitizeOriginalFileName(input);

        Assert.Equal(expected, result);
    }
}
