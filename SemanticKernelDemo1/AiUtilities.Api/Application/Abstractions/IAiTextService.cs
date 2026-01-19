namespace AiUtilities.Application.Abstractions;

public interface IAiTextService
{
    Task<string> SummarizeAsync(string text, int bulletCount = 3);
    Task<string> RewriteAsync(string text, string tone = "professional");
    Task<string> ClassifyAsync(string text);
}