using DbUp;
using LittleMunchkins.Api.Data;
using LittleMunchkins.Api.Endpoints;
using LittleMunchkins.Api.Services;
using LittleMunchkins.Api.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ── Connection string ───────────────────────────────────────────────────────
var rawConnStr = builder.Configuration["DATABASE_URL"]
    ?? throw new Exception("DATABASE_URL is required");

// Railway injects a postgres:// URI; translate to Npgsql format if needed
string npgsqlConnStr;
if (rawConnStr.StartsWith("postgres://") || rawConnStr.StartsWith("postgresql://"))
{
    var uri = new Uri(rawConnStr);
    var info = uri.UserInfo.Split(':');
    npgsqlConnStr = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = info[0],
        Password = info.Length > 1 ? info[1] : null,
        SslMode = SslMode.Prefer,
    }.ConnectionString;
}
else npgsqlConnStr = rawConnStr;

// ── Run DbUp migrations ────────────────────────────────────────────────────
EnsureDatabase.For.PostgresqlDatabase(npgsqlConnStr);
var upgrader = DeployChanges.To
    .PostgresqlDatabase(npgsqlConnStr)
    .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly)
    .LogToConsole()
    .Build();
var result = upgrader.PerformUpgrade();
if (!result.Successful) throw result.Error;

// ── Auth (Clerk JWTs) ───────────────────────────────────────────────────────
var clerkDomain = builder.Configuration["CLERK_DOMAIN"]
    ?? "https://clerk.example.com"; // override via env

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = clerkDomain;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            NameClaimType = "sub",
        };
    });
builder.Services.AddAuthorization();

// ── DI ─────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionFactory>(_ => new NpgsqlConnectionFactory(npgsqlConnStr));
builder.Services.AddScoped<SessionRepository>();
builder.Services.AddSingleton<BucketClient>();
builder.Services.AddSingleton<ClaudeClient>();
builder.Services.AddSingleton<TranscriptionService>();
builder.Services.AddSingleton<VideoPreprocessor>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<AnalyzeSessionWorker>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()));

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapUploads();
app.MapSessions();
app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
