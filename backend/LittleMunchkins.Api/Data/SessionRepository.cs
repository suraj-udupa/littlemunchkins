using Dapper;
using LittleMunchkins.Api.Data;

namespace LittleMunchkins.Api.Data;

public record SessionRow(Guid Id, string Status, string? LlmResponse, string? ErrorMessage);
public record JobRow(Guid Id, Guid SessionId);

public class SessionRepository(IConnectionFactory db)
{
    public async Task<Guid> GetOrCreateUserAsync(string clerkId)
    {
        using var conn = db.Create();
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>(SqlQueries.Users.GetByClerkId, new { ClerkId = clerkId });
        if (id.HasValue) return id.Value;
        return await conn.QuerySingleAsync<Guid>(SqlQueries.Users.UpsertByClerkId, new { ClerkId = clerkId });
    }

    public async Task<Guid> InsertMediaAsync(Guid userId, string bucketKey, string contentType)
    {
        using var conn = db.Create();
        return await conn.QuerySingleAsync<Guid>(SqlQueries.Media.Insert, new { UserId = userId, BucketKey = bucketKey, ContentType = contentType });
    }

    public async Task<Guid> InsertSessionAsync(Guid userId, string? childAge, string? questionText, Guid? mediaId)
    {
        using var conn = db.Create();
        return await conn.QuerySingleAsync<Guid>(SqlQueries.Sessions.Insert, new { UserId = userId, ChildAge = childAge, QuestionText = questionText, MediaId = mediaId });
    }

    public async Task InsertJobAsync(Guid sessionId)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(SqlQueries.Jobs.Insert, new { SessionId = sessionId });
    }

    public async Task<SessionRow?> GetSessionAsync(Guid id)
    {
        using var conn = db.Create();
        return await conn.QuerySingleOrDefaultAsync<SessionRow>(SqlQueries.Sessions.GetById, new { Id = id });
    }

    public async Task SetSessionStatusAsync(Guid id, string status)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(SqlQueries.Sessions.UpdateStatus, new { Id = id, Status = status });
    }

    public async Task SetSessionResultAsync(Guid id, string llmResponseJson)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(SqlQueries.Sessions.SetResult, new { Id = id, LlmResponse = llmResponseJson });
    }

    public async Task SetSessionErrorAsync(Guid id, string error)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(SqlQueries.Sessions.SetError, new { Id = id, ErrorMessage = error });
    }

    public async Task<JobRow?> ClaimNextJobAsync(string workerId)
    {
        using var conn = db.Create();
        return await conn.QuerySingleOrDefaultAsync<JobRow>(SqlQueries.Jobs.ClaimNext, new { WorkerId = workerId });
    }

    public async Task CompleteJobAsync(Guid jobId)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(SqlQueries.Jobs.Complete, new { Id = jobId });
    }

    public async Task FailJobAsync(Guid jobId)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(SqlQueries.Jobs.Fail, new { Id = jobId });
    }
}
