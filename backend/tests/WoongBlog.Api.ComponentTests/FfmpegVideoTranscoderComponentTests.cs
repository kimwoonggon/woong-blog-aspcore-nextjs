using Microsoft.Extensions.Options;
using WoongBlog.Application.Modules.Content.Works.WorkVideos;
using WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

namespace WoongBlog.Api.Tests;

[Trait(TestCategories.Key, TestCategories.Component)]
public sealed class FfmpegVideoTranscoderComponentTests
{
    [Fact]
    public async Task SegmentHlsAsync_FallsBackToCompatibleTranscode_WhenCopyModeFails()
    {
        var fakeFfTools = CreateCopyFailingFakeFfTools();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"portfolio-tests-hls-fallback-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDirectory);
            var inputPath = Path.Combine(tempDirectory, "source.mp4");
            var hlsDirectory = Path.Combine(tempDirectory, "hls");
            await File.WriteAllBytesAsync(inputPath, SampleMp4Bytes);
            Directory.CreateDirectory(hlsDirectory);

            var transcoder = new FfmpegVideoTranscoder(Options.Create(new WorkVideoHlsOptions
            {
                FfmpegPath = fakeFfTools.FfmpegPath,
                FfprobePath = fakeFfTools.FfprobePath,
                SegmentDurationSeconds = 4,
                TimelinePreviewIntervalSeconds = 5,
                TimelinePreviewTileColumns = 4,
                TimeoutSeconds = 30
            }));

            var error = await transcoder.SegmentHlsAsync(inputPath, hlsDirectory, "master.m3u8", CancellationToken.None);

            Assert.Null(error);
            Assert.True(File.Exists(Path.Combine(hlsDirectory, "master.m3u8")));
            Assert.True(File.Exists(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewSpriteFileName)));
            Assert.True(File.Exists(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewVttFileName)));
            var invocations = ReadFfmpegInvocations(fakeFfTools.ArgsPath);
            Assert.Collection(
                invocations.Take(2),
                copyArgs =>
                {
                    Assert.Contains("-c", copyArgs);
                    Assert.Contains("copy", copyArgs);
                },
                fallbackArgs =>
                {
                    Assert.Contains("-c:v", fallbackArgs);
                    Assert.Contains("libx264", fallbackArgs);
                    Assert.Contains("-pix_fmt", fallbackArgs);
                    Assert.Contains("yuv420p", fallbackArgs);
                    Assert.Contains("-c:a", fallbackArgs);
                    Assert.Contains("aac", fallbackArgs);
                });
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }

            if (Directory.Exists(fakeFfTools.DirectoryPath))
            {
                Directory.Delete(fakeFfTools.DirectoryPath, recursive: true);
            }
        }
    }

    private static FakeFfTools CreateCopyFailingFakeFfTools()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"portfolio-tests-ffmpeg-fallback-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var ffmpegPath = Path.Combine(directory, "ffmpeg");
        var ffprobePath = Path.Combine(directory, "ffprobe");
        var argsPath = Path.Combine(directory, "args.txt");
        var countPath = Path.Combine(directory, "count.txt");
        var escapedArgsPath = QuoteShellValue(argsPath);
        var escapedCountPath = QuoteShellValue(countPath);

        File.WriteAllText(ffmpegPath, $$"""
#!/bin/sh
count=0
if [ -f {{escapedCountPath}} ]; then
  count=$(cat {{escapedCountPath}})
fi
count=$((count + 1))
printf '%s' "$count" > {{escapedCountPath}}
{
  printf '%s\n' "-- invocation $count --"
  printf '%s\n' "$@"
} >> {{escapedArgsPath}}
if [ "$count" -eq 1 ]; then
  printf '%s\n' 'copy-mode mux failed' >&2
  exit 1
fi
out=""
for arg in "$@"; do out="$arg"; done
dir=$(dirname "$out")
mkdir -p "$dir"
case "$out" in
  *.jpg)
    printf 'sprite' > "$out"
    ;;
  *)
    cat > "$out" <<'M3U8'
#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:1
#EXT-X-MEDIA-SEQUENCE:0
#EXTINF:1.0,
segment_00000.ts
#EXT-X-ENDLIST
M3U8
    printf 'segment' > "$dir/segment_00000.ts"
    ;;
esac
""");
        File.WriteAllText(ffprobePath, """
#!/bin/sh
printf '20.0'
""");

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                ffmpegPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.SetUnixFileMode(
                ffprobePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return new FakeFfTools(directory, ffmpegPath, ffprobePath, argsPath);
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadFfmpegInvocations(string argsPath)
    {
        var invocations = new List<IReadOnlyList<string>>();
        var current = new List<string>();
        foreach (var line in File.ReadAllLines(argsPath))
        {
            if (line.StartsWith("-- invocation ", StringComparison.Ordinal))
            {
                if (current.Count > 0)
                {
                    invocations.Add(current.ToArray());
                    current.Clear();
                }

                continue;
            }

            current.Add(line);
        }

        if (current.Count > 0)
        {
            invocations.Add(current.ToArray());
        }

        return invocations;
    }

    private static string QuoteShellValue(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
    }

    private sealed record FakeFfTools(string DirectoryPath, string FfmpegPath, string FfprobePath, string ArgsPath);

    private static readonly byte[] SampleMp4Bytes =
    [
        0x00, 0x00, 0x00, 0x18,
        0x66, 0x74, 0x79, 0x70,
        0x6D, 0x70, 0x34, 0x32,
        0x00, 0x00, 0x00, 0x00,
        0x6D, 0x70, 0x34, 0x32,
        0x69, 0x73, 0x6F, 0x6D
    ];
}
