namespace historydemo1.Models
{
    public interface IConversationSummaryService
    {
        Task TryUpdateSummaryAsync(
            string conversationId,
            string userId,
            CancellationToken cancellationToken = default);
    }
}
