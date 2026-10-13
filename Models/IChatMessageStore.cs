using historydemo1.Controllers;

namespace historydemo1.Models
{
    //public interface IChatMessageStore
    //{
    //    Task<IReadOnlyList<StoredChatMessage>> LoadRecentAsync(
    //        string conversationId,
    //        int count,
    //        CancellationToken cancellationToken);

    //    Task<List<StoredChatMessage>> LoadAfterAsync(
    //       string conversationId,
    //       long messageId,
    //       CancellationToken cancellationToken);
    //    // 从指定消息 ID 之前，向历史方向分页读取。
    //    Task<List<AgentMessageEntity>> LoadBeforeAsync(
    //        string conversationId,
    //         long afterMessageId,
    //        long beforeMessageId,
    //        int pageSize,
    //        CancellationToken cancellationToken);

    //    Task AppendAsync(
    //        string conversationId,
    //        IReadOnlyList<StoredChatMessage> messages,
    //        CancellationToken cancellationToken);
    //}
    public interface IChatMessageStore
    {
        Task AppendAsync(
            AgentMessageEntity message,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AgentMessageEntity>> LoadAfterAsync(
            string conversationId,
            long afterMessageId,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AgentMessageEntity>> LoadRecentTurnsAsync(
            string conversationId,
            long afterMessageId,
            int pageSize,
            CancellationToken cancellationToken = default);
    }

}
