using historydemo1.Controllers;

namespace historydemo1.Models
{
    public interface IChatMessageStore
    {
        Task<IReadOnlyList<StoredChatMessage>> LoadRecentAsync(
            string conversationId,
            int count,
            CancellationToken cancellationToken);

        //Task<List<StoredChatMessage>>  LoadAfterAsync(
        //   string conversationId,
        //   long messageId,
        //   CancellationToken cancellationToken);

        Task AppendAsync(
            string conversationId,
            IReadOnlyList<StoredChatMessage> messages,
            CancellationToken cancellationToken);
    }
}
