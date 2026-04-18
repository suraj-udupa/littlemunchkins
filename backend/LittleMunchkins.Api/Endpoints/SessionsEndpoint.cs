using System.Security.Claims;
using System.Text.Json;
using LittleMunchkins.Api.Data;

namespace LittleMunchkins.Api.Endpoints;

public static class SessionsEndpoint
{
    public static void MapSessions(this WebApplication app)
    {
        app.MapPost("/api/sessions", async (CreateSessionRequest req, ClaimsPrincipal user, SessionRepository repo) =>
        {
            var clerkId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
                ?? throw new Exception("No user identity");

            var userId = await repo.GetOrCreateUserAsync(clerkId);
            var sessionId = await repo.InsertSessionAsync(userId, req.ChildAge, req.QuestionText, req.MediaId);
            await repo.InsertJobAsync(sessionId);

            return Results.Ok(new { sessionId });
        }).RequireAuthorization();

        app.MapGet("/api/sessions/{id:guid}", async (Guid id, SessionRepository repo) =>
        {
            var session = await repo.GetSessionAsync(id);
            if (session is null) return Results.NotFound();

            object? result = null;
            if (session.Status == "complete" && session.LlmResponse is not null)
                result = JsonSerializer.Deserialize<object>(session.LlmResponse);

            return Results.Ok(new { status = session.Status, result });
        }).RequireAuthorization();
    }

    private record CreateSessionRequest(string? QuestionText, Guid? MediaId, string? ChildAge);
}
