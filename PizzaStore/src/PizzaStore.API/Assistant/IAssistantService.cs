namespace PizzaStore.API.Assistant;

public interface IAssistantService
{
    Task<AssistantChatResponse> ChatAsync(AssistantChatRequest request, CancellationToken cancellationToken);
}
