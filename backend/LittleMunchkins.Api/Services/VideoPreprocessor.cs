using System.Diagnostics;

namespace LittleMunchkins.Api.Services;

public class VideoPreprocessor
{
    public async Task<(List<string> FramePaths, string AudioPath)> ExtractAsync(string videoPath, string workDir)
    {
        Directory.CreateDirectory(workDir);

        var framePattern = Path.Combine(workDir, "frame_%03d.jpg");
        await RunFfmpegAsync($"-i \"{videoPath}\" -vf fps=1/5 -vframes 6 \"{framePattern}\"");

        var audioPath = Path.Combine(workDir, "audio.webm");
        await RunFfmpegAsync($"-i \"{videoPath}\" -vn -acodec libopus \"{audioPath}\"");

        var frames = Directory.GetFiles(workDir, "frame_*.jpg").OrderBy(f => f).ToList();
        return (frames, audioPath);
    }

    private static async Task RunFfmpegAsync(string args)
    {
        var psi = new ProcessStartInfo("ffmpeg", args)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi) ?? throw new Exception("Failed to start ffmpeg");
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new Exception($"ffmpeg failed: {err}");
        }
    }
}
