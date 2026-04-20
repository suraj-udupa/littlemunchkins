using System.Text.Json;
using LittleMunchkins.Api.Data;
using LittleMunchkins.Api.Services;

namespace LittleMunchkins.Api.Workers;

public class AnalyzeSessionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyzeSessionWorker> logger) : BackgroundService
{
    private static readonly string WorkerId = Environment.MachineName;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker loop error");
            }
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }

    private async Task ProcessNextJobAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SessionRepository>();
        var bucket = scope.ServiceProvider.GetRequiredService<BucketClient>();
        var claude = scope.ServiceProvider.GetRequiredService<ClaudeClient>();
        var transcription = scope.ServiceProvider.GetRequiredService<TranscriptionService>();
        var video = scope.ServiceProvider.GetRequiredService<VideoPreprocessor>();

        var job = await repo.ClaimNextJobAsync(WorkerId);
        if (job is null) return;

        await repo.SetSessionStatusAsync(job.SessionId, "processing");

        try
        {
            var session = await repo.GetSessionAsync(job.SessionId)
                          ?? throw new Exception($"Session {job.SessionId} not found");

            // Re-fetch full session details via a dedicated query (extend as needed)
            // For now we pull the raw row and read fields from the DB directly via SQL
            using var conn = scope.ServiceProvider.GetRequiredService<IConnectionFactory>().Create();
            var detail = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<SessionDetail>(conn,
                "SELECT question_text, child_age, media_id FROM sessions WHERE id = @Id", new { Id = job.SessionId });

            if (detail is null) throw new Exception("Session detail not found");

            BehaviorAnalysis result;

            if (detail.media_id is null)
            {
                result = await claude.AnalyseTextAsync(detail.question_text ?? "", detail.child_age);
            }
            else
            {
                var mediaRow = await Dapper.SqlMapper.QuerySingleAsync<MediaDetail>(conn,
                    "SELECT bucket_key, content_type FROM media WHERE id = @Id", new { Id = detail.media_id });

                var isVideo = mediaRow.content_type.StartsWith("video/");
                var isAudio = mediaRow.content_type.StartsWith("audio/");
                var isImage = mediaRow.content_type.StartsWith("image/");

                if (isImage)
                {
                    await using var stream = await bucket.GetObjectStreamAsync(mediaRow.bucket_key);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    result = await claude.AnalyseImageAsync(ms.ToArray(), mediaRow.content_type, detail.question_text, detail.child_age);
                }
                else if (isAudio)
                {
                    await using var stream = await bucket.GetObjectStreamAsync(mediaRow.bucket_key);
                    var transcript = await transcription.TranscribeAsync(stream, Path.GetFileName(mediaRow.bucket_key));
                    result = await claude.AnalyseTranscriptAsync(transcript, detail.question_text, detail.child_age);
                }
                else if (isVideo)
                {
                    var tmpDir = Path.Combine(Path.GetTempPath(), job.SessionId.ToString());
                    var videoPath = Path.Combine(tmpDir, "input.mp4");
                    Directory.CreateDirectory(tmpDir);

                    await using (var stream = await bucket.GetObjectStreamAsync(mediaRow.bucket_key))
                    await using (var fs = File.Create(videoPath))
                        await stream.CopyToAsync(fs);

                    var (framePaths, audioPath) = await video.ExtractAsync(videoPath, Path.Combine(tmpDir, "out"));

                    var frames = framePaths
                        .Take(6)
                        .Select(p => (File.ReadAllBytes(p), "image/jpeg"))
                        .ToList();

                    string transcript;
                    if (File.Exists(audioPath))
                    {
                        await using var audioStream = File.OpenRead(audioPath);
                        transcript = await transcription.TranscribeAsync(audioStream, "audio.webm");
                    }
                    else transcript = string.Empty;

                    result = await claude.AnalyseVideoFramesAsync(frames, transcript, detail.question_text, detail.child_age);

                    Directory.Delete(tmpDir, recursive: true);
                }
                else throw new Exception($"Unsupported media type: {mediaRow.content_type}");
            }

            var json = JsonSerializer.Serialize(result);
            await repo.SetSessionResultAsync(job.SessionId, json);
            await repo.CompleteJobAsync(job.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed", job.Id);
            await repo.SetSessionErrorAsync(job.SessionId, ex.Message);
            await repo.FailJobAsync(job.Id);
        }
    }

    private record SessionDetail(string? question_text, string? child_age, Guid? media_id);
    private record MediaDetail(string bucket_key, string content_type);
}
