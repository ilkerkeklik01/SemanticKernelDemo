namespace AiUtilities.Application.Abstractions;

public interface IAiTextService
{
    Task<string> SummarizeAsync(string text, int bulletCount = 3);
}