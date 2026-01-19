using AiUtilities.Application.Abstractions;
using AiUtilities.Infrastructure.Ai;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAiTextService, GeminiAiTextService>();

var app = builder.Build();

app.MapPost("/summarize", async (
    SummarizeRequest request,
    IAiTextService aiTextService) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest("Text is required");

    var summary = await aiTextService.SummarizeAsync(
        request.Text,
        request.BulletCount ?? 3);

    return Results.Ok(new { summary });
});

app.MapPost("/rewrite", async (RewriteRequest request, IAiTextService ai) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest("Text is required");

    var rewritten = await ai.RewriteAsync(request.Text, request.Tone ?? "professional");
    return Results.Ok(new { rewritten });
});

app.MapPost("/classify", async (ClassifyRequest request, IAiTextService ai) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest("Text is required");

    var label = await ai.ClassifyAsync(request.Text);
    return Results.Ok(new { label });
});

//add swagger
app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();


record SummarizeRequest(string Text, int? BulletCount);
record RewriteRequest(string Text, string? Tone);
record ClassifyRequest(string Text);