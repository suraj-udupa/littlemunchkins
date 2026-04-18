using LittleMunchkins.Api.Data;
using LittleMunchkins.Api.Services;
using System.Security.Claims;

namespace LittleMunchkins.Api.Endpoints;

public static class UploadsEndpoint
{
    public static void MapUploads(this WebApplication app)
    {
        app.MapPost("/api/uploads/sign", async (SignUploadRequest req, ClaimsPrincipal user, SessionRepository repo, BucketClient bucket) =>
        {
            var clerkId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
                ?? throw new Exception("No user identity");

            var userId = await repo.GetOrCreateUserAsync(clerkId);
            var ext = Path.GetExtension(req.Filename);
            var objectKey = $"{userId}/{Guid.NewGuid()}{ext}";

            var uploadUrl = await bucket.GenerateUploadUrlAsync(objectKey, req.ContentType);
            var mediaId = await repo.InsertMediaAsync(userId, objectKey, req.ContentType);

            return Results.Ok(new { uploadUrl, id = mediaId });
        }).RequireAuthorization();
    }

    private record SignUploadRequest(string Filename, string ContentType);
}
