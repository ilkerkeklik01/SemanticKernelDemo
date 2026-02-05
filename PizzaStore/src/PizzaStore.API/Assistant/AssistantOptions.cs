namespace PizzaStore.API.Assistant;

public class AssistantOptions
{
    public string? ApiBaseUrl { get; set; }
    public int MaxTokens { get; set; } = 800;
    public double Temperature { get; set; } = 0.2;
}
