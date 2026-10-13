namespace historydemo1.Models
{
    public interface IConversationSummaryRepository
    {
        Task<ConversationSummaryEntity?> GetAsync(
            string conversationId,
            CancellationToken cancellationToken);

        //Task<List<SummaryInputMessage>> GetMessagesAfterAsync(
        //    string conversationId,
        //    long messageId,
        //    CancellationToken cancellationToken);

        Task<bool> TrySaveAsync(
            ConversationSummaryEntity summary,
            int expectedVersion,
            CancellationToken cancellationToken);
    }
}
