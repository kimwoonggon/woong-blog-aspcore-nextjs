using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

namespace WoongBlog.Api.Modules.Content.Works.Application.WorkVideos;

public sealed class FfmpegVideoTranscoder(IOptions<WorkVideoHlsOptions> hlsOptions) : IVideoTranscoder
{
    private readonly WorkVideoHlsOptions _hlsOptions = hlsOptions.Value;

    public async Task<string?> SegmentHlsAsync(
        string inputPath,
        string hlsDirectory,
        string manifestFileName,
        CancellationToken cancellationToken)
    {
        var segmentDuration = Math.Max(1, _hlsOptions.SegmentDurationSeconds);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _hlsOptions.TimeoutSeconds)));

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
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("-map");
        startInfo.ArgumentList.Add("0:v:0");
        startInfo.ArgumentList.Add("-map");
        startInfo.ArgumentList.Add("0:a:0?");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("copy");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("hls");
        startInfo.ArgumentList.Add("-hls_time");
        startInfo.ArgumentList.Add(segmentDuration.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-hls_playlist_type");
        startInfo.ArgumentList.Add("vod");
        startInfo.ArgumentList.Add("-hls_segment_filename");
        startInfo.ArgumentList.Add("segment_%05d.ts");
        startInfo.ArgumentList.Add(manifestFileName);

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
                return "Unable to start HLS processing.";
            }

            process.BeginErrorReadLine();
            await process.WaitForExitAsync(timeout.Token);
            process.WaitForExit();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKillProcess(process);
            return "HLS processing timed out.";
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return $"Unable to start HLS processing: {exception.Message}";
        }

        if (process.ExitCode != 0)
        {
            var message = stderr.ToString().Trim();
            return string.IsNullOrWhiteSpace(message)
                ? "Unable to process MP4 into HLS."
                : $"Unable to process MP4 into HLS: {message}";
        }

        return File.Exists(Path.Combine(hlsDirectory, manifestFileName))
            ? null
            : "HLS processing did not produce a manifest.";
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
