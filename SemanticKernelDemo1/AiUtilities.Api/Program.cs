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
//add swagger
app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();


record SummarizeRequest(string Text, int? BulletCount);
