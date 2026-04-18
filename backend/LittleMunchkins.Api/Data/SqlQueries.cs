namespace LittleMunchkins.Api.Data;

public static class SqlQueries
{
    public static class Users
    {
        public const string UpsertByClerkId = """
            INSERT INTO users (clerk_id) VALUES (@ClerkId)
            ON CONFLICT (clerk_id) DO NOTHING
            RETURNING id;
            """;

        public const string GetByClerkId = "SELECT id FROM users WHERE clerk_id = @ClerkId";
    }

    public static class Media
    {
        public const string Insert = """
            INSERT INTO media (user_id, bucket_key, content_type)
            VALUES (@UserId, @BucketKey, @ContentType)
            RETURNING id;
            """;
    }

    public static class Sessions
    {
        public const string Insert = """
            INSERT INTO sessions (user_id, child_age, question_text, media_id)
            VALUES (@UserId, @ChildAge, @QuestionText, @MediaId)
            RETURNING id;
            """;

        public const string GetById = """
            SELECT id, status, llm_response AS LlmResponse, error_message AS ErrorMessage
            FROM sessions WHERE id = @Id;
            """;

        public const string UpdateStatus = """
            UPDATE sessions SET status = @Status, updated_at = now() WHERE id = @Id;
            """;

        public const string SetResult = """
            UPDATE sessions
            SET status = 'complete', llm_response = @LlmResponse::jsonb, updated_at = now()
            WHERE id = @Id;
            """;

        public const string SetError = """
            UPDATE sessions
            SET status = 'error', error_message = @ErrorMessage, updated_at = now()
            WHERE id = @Id;
            """;
    }

    public static class Jobs
    {
        public const string Insert = """
            INSERT INTO jobs (session_id) VALUES (@SessionId) RETURNING id;
            """;

        public const string ClaimNext = """
            UPDATE jobs SET status = 'processing', locked_at = now(), locked_by = @WorkerId, attempts = attempts + 1
            WHERE id = (
                SELECT id FROM jobs
                WHERE status = 'pending' AND attempts < 3
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            )
            RETURNING id, session_id AS SessionId;
            """;

        public const string Complete = "UPDATE jobs SET status = 'complete' WHERE id = @Id;";
        public const string Fail = "UPDATE jobs SET status = 'failed' WHERE id = @Id;";
    }
}
