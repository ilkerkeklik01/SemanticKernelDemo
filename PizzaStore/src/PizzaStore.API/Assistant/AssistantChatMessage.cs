namespace PizzaStore.API.Assistant;

public class AssistantChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}
