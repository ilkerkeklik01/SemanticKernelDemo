using AiUtilities.Application.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

#pragma warning disable SKEXP0070

namespace AiUtilities.Infrastructure.Ai;

public sealed class GeminiAiTextService : IAiTextService
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _summarizeFunction;
    private readonly KernelFunction _rewriteFunction;
    private readonly KernelFunction _classifyFunction;

    public GeminiAiTextService()
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                     ?? throw new InvalidOperationException("GEMINI_API_KEY not found");

        var builder = Kernel.CreateBuilder();

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: "gemini-2.5-flash-lite",
            apiKey: apiKey
        );

        _kernel = builder.Build();

        var baseDir = AppContext.BaseDirectory;
        var promptsPath = Path.Combine(baseDir, "Prompts", "Text");

        var textPlugin = _kernel.CreatePluginFromPromptDirectory(promptsPath, pluginName: "Text");
        _kernel.Plugins.Add(textPlugin);

        _summarizeFunction = textPlugin["Summarize"];
        _rewriteFunction = textPlugin["Rewrite"];
        _classifyFunction = textPlugin["Classify"];
    }

    public async Task<string> SummarizeAsync(string text, int bulletCount = 3)
    {
        var result = await _kernel.InvokeAsync(
            _summarizeFunction,
            new KernelArguments
            {
                ["text"] = text,
                ["bulletCount"] = bulletCount
            });

        return result.ToString();
    }

    public async Task<string> RewriteAsync(string text, string tone = "professional")
    {
        var result = await _kernel.InvokeAsync(_rewriteFunction, new KernelArguments
        {
            ["text"] = text,
            ["tone"] = tone
        });

        return result.ToString();
    }

    public async Task<string> ClassifyAsync(string text)
    {
        var result = await _kernel.InvokeAsync(_classifyFunction, new KernelArguments
        {
            ["text"] = text
        });

        return result.ToString().Trim();
    }

}
