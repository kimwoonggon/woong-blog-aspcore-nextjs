using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;

namespace WoongBlog.Infrastructure.Modules.Content.Works.WorkVideos;

public sealed class FfmpegVideoTranscoder(IOptions<WorkVideoHlsOptions> hlsOptions) : IVideoTranscoder
{
    private readonly WorkVideoHlsOptions _hlsOptions = hlsOptions.Value;
    private enum FfmpegProcessStatus
    {
        Succeeded,
        Failed,
        StartFailed,
        TimedOut
    }

    private sealed record FfmpegProcessResult(FfmpegProcessStatus Status, string? ErrorMessage);

    public async Task<string?> SegmentHlsAsync(
        string inputPath,
        string hlsDirectory,
        string manifestFileName,
        CancellationToken cancellationToken)
    {
        var segmentDuration = Math.Max(1, _hlsOptions.SegmentDurationSeconds);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _hlsOptions.TimeoutSeconds)));

        var copyResult = await RunFfmpegProcessAsync(
            CreateHlsStartInfo(inputPath, hlsDirectory, manifestFileName, segmentDuration, useCompatibilityTranscode: false),
            timeout.Token,
            cancellationToken);
        if (copyResult.Status == FfmpegProcessStatus.Succeeded)
        {
            return await FinalizeSuccessfulHlsAsync(inputPath, hlsDirectory, manifestFileName, cancellationToken);
        }

        if (copyResult.Status is FfmpegProcessStatus.StartFailed or FfmpegProcessStatus.TimedOut)
        {
            return copyResult.ErrorMessage;
        }

        DeletePartialHlsOutputs(hlsDirectory, manifestFileName);

        var compatibilityResult = await RunFfmpegProcessAsync(
            CreateHlsStartInfo(inputPath, hlsDirectory, manifestFileName, segmentDuration, useCompatibilityTranscode: true),
            timeout.Token,
            cancellationToken);
        if (compatibilityResult.Status == FfmpegProcessStatus.Succeeded)
        {
            return await FinalizeSuccessfulHlsAsync(inputPath, hlsDirectory, manifestFileName, cancellationToken);
        }

        if (compatibilityResult.Status is FfmpegProcessStatus.StartFailed or FfmpegProcessStatus.TimedOut)
        {
            return compatibilityResult.ErrorMessage;
        }

        return BuildHlsProcessingError(copyResult.ErrorMessage, compatibilityResult.ErrorMessage);
    }

    private ProcessStartInfo CreateHlsStartInfo(
        string inputPath,
        string hlsDirectory,
        string manifestFileName,
        int segmentDuration,
        bool useCompatibilityTranscode)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _hlsOptions.FfmpegPath,
            WorkingDirectory = hlsDirectory,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-loglevel");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("-map");
        startInfo.ArgumentList.Add("0:v:0");
        startInfo.ArgumentList.Add("-map");
        startInfo.ArgumentList.Add("0:a:0?");
        if (useCompatibilityTranscode)
        {
            startInfo.ArgumentList.Add("-c:v");
            startInfo.ArgumentList.Add("libx264");
            startInfo.ArgumentList.Add("-preset");
            startInfo.ArgumentList.Add("veryfast");
            startInfo.ArgumentList.Add("-profile:v");
            startInfo.ArgumentList.Add("main");
            startInfo.ArgumentList.Add("-pix_fmt");
            startInfo.ArgumentList.Add("yuv420p");
            startInfo.ArgumentList.Add("-c:a");
            startInfo.ArgumentList.Add("aac");
            startInfo.ArgumentList.Add("-b:a");
            startInfo.ArgumentList.Add("128k");
            startInfo.ArgumentList.Add("-ac");
            startInfo.ArgumentList.Add("2");
        }
        else
        {
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("copy");
        }

        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("hls");
        startInfo.ArgumentList.Add("-hls_time");
        startInfo.ArgumentList.Add(segmentDuration.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-hls_playlist_type");
        startInfo.ArgumentList.Add("vod");
        startInfo.ArgumentList.Add("-hls_segment_filename");
        startInfo.ArgumentList.Add("segment_%05d.ts");
        startInfo.ArgumentList.Add(manifestFileName);

        return startInfo;
    }

    private static async Task<FfmpegProcessResult> RunFfmpegProcessAsync(
        ProcessStartInfo startInfo,
        CancellationToken timeoutToken,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        var stderr = new StringBuilder();
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                stderr.AppendLine(args.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                return new FfmpegProcessResult(FfmpegProcessStatus.StartFailed, "Unable to start HLS processing.");
            }

            process.BeginErrorReadLine();
            await process.WaitForExitAsync(timeoutToken);
            process.WaitForExit();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKillProcess(process);
            return new FfmpegProcessResult(FfmpegProcessStatus.TimedOut, "HLS processing timed out.");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new FfmpegProcessResult(
                FfmpegProcessStatus.StartFailed,
                $"Unable to start HLS processing: {exception.Message}");
        }

        return process.ExitCode == 0
            ? new FfmpegProcessResult(FfmpegProcessStatus.Succeeded, null)
            : new FfmpegProcessResult(FfmpegProcessStatus.Failed, stderr.ToString().Trim());
    }

    private async Task<string?> FinalizeSuccessfulHlsAsync(
        string inputPath,
        string hlsDirectory,
        string manifestFileName,
        CancellationToken cancellationToken)
    {
        await TryGenerateTimelinePreviewAsync(inputPath, hlsDirectory, cancellationToken);

        return File.Exists(Path.Combine(hlsDirectory, manifestFileName))
            ? null
            : "HLS processing did not produce a manifest.";
    }

    private static void DeletePartialHlsOutputs(string hlsDirectory, string manifestFileName)
    {
        DeleteIfExists(Path.Combine(hlsDirectory, manifestFileName));
        DeleteIfExists(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewVttFileName));
        DeleteIfExists(Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewSpriteFileName));

        foreach (var segmentPath in Directory.EnumerateFiles(hlsDirectory, "segment_*.ts"))
        {
            DeleteIfExists(segmentPath);
        }
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string BuildHlsProcessingError(string? copyError, string? compatibilityError)
    {
        var copyMessage = NormalizeProcessError(copyError);
        var compatibilityMessage = NormalizeProcessError(compatibilityError);
        if (copyMessage.Length == 0 && compatibilityMessage.Length == 0)
        {
            return "Unable to process MP4 into HLS.";
        }

        if (copyMessage.Length == 0)
        {
            return $"Unable to process MP4 into HLS: compatibility transcode failed: {compatibilityMessage}";
        }

        if (compatibilityMessage.Length == 0)
        {
            return $"Unable to process MP4 into HLS: {copyMessage}";
        }

        return $"Unable to process MP4 into HLS: copy mode failed: {copyMessage}; compatibility transcode failed: {compatibilityMessage}";
    }

    private static string NormalizeProcessError(string? message)
    {
        const int maxLength = 2_000;
        var trimmed = message?.Trim() ?? string.Empty;
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return string.Concat(trimmed.AsSpan(0, maxLength), "...");
    }

    private async Task TryGenerateTimelinePreviewAsync(
        string inputPath,
        string hlsDirectory,
        CancellationToken cancellationToken)
    {
        var durationSeconds = await TryProbeDurationAsync(inputPath, cancellationToken);
        if (durationSeconds is null || durationSeconds.Value <= 0)
        {
            return;
        }

        var intervalSeconds = Math.Max(1, _hlsOptions.TimelinePreviewIntervalSeconds);
        var frameCount = Math.Max(1, Math.Min(
            _hlsOptions.TimelinePreviewMaxFrames,
            (int)Math.Ceiling(durationSeconds.Value / intervalSeconds)));
        var columns = Math.Max(1, Math.Min(_hlsOptions.TimelinePreviewTileColumns, frameCount));
        var rows = (int)Math.Ceiling(frameCount / (double)columns);
        var spritePath = Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewSpriteFileName);

        var previewStartInfo = new ProcessStartInfo
        {
            FileName = _hlsOptions.FfmpegPath,
            WorkingDirectory = hlsDirectory,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        previewStartInfo.ArgumentList.Add("-hide_banner");
        previewStartInfo.ArgumentList.Add("-loglevel");
        previewStartInfo.ArgumentList.Add("error");
        previewStartInfo.ArgumentList.Add("-i");
        previewStartInfo.ArgumentList.Add(inputPath);
        previewStartInfo.ArgumentList.Add("-vf");
        previewStartInfo.ArgumentList.Add(frameCount == 1
            ? "scale=320:180:force_original_aspect_ratio=decrease,pad=320:180:(ow-iw)/2:(oh-ih)/2:black"
            : $"fps=1/{intervalSeconds},scale=320:180:force_original_aspect_ratio=decrease,pad=320:180:(ow-iw)/2:(oh-ih)/2:black,tile={columns}x{rows}");
        previewStartInfo.ArgumentList.Add("-frames:v");
        previewStartInfo.ArgumentList.Add(frameCount.ToString(CultureInfo.InvariantCulture));
        previewStartInfo.ArgumentList.Add("-q:v");
        previewStartInfo.ArgumentList.Add("4");
        previewStartInfo.ArgumentList.Add("-y");
        previewStartInfo.ArgumentList.Add(WorkVideoPolicy.TimelinePreviewSpriteFileName);

        var previewSucceeded = await RunQuietProcessAsync(previewStartInfo, cancellationToken);
        if (!previewSucceeded || !File.Exists(spritePath))
        {
            return;
        }

        var vttPath = Path.Combine(hlsDirectory, WorkVideoPolicy.TimelinePreviewVttFileName);
        await File.WriteAllTextAsync(
            vttPath,
            BuildTimelinePreviewVtt(frameCount, columns, intervalSeconds, durationSeconds.Value),
            cancellationToken);
    }

    private async Task<double?> TryProbeDurationAsync(string inputPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _hlsOptions.FfprobePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("format=duration");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
        startInfo.ArgumentList.Add(inputPath);

        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                return null;
            }

            return double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var duration)
                ? duration
                : null;
        }
        catch
        {
            return null;
        }
    }

    internal static string BuildTimelinePreviewVtt(int frameCount, int columns, int intervalSeconds, double durationSeconds)
    {
        var builder = new StringBuilder("WEBVTT\n\n");
        for (var index = 0; index < frameCount; index++)
        {
            var start = index * intervalSeconds;
            var end = Math.Min(durationSeconds, (index + 1) * intervalSeconds);
            if (end <= start)
            {
                break;
            }

            var x = (index % columns) * 320;
            var y = (index / columns) * 180;
            builder.AppendLine($"{FormatVttTimestamp(start)} --> {FormatVttTimestamp(end)}");
            builder.AppendLine($"timeline-sprite.jpg#xywh={x},{y},320,180");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatVttTimestamp(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
    }

    private static async Task<bool> RunQuietProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
