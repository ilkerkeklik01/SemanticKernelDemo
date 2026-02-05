namespace PizzaStore.API.Assistant;

public class AssistantChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<AssistantChatMessage>? History { get; set; }
}
