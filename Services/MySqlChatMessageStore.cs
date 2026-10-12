using historydemo1.Models;
using SqlSugar;

namespace historydemo1.Services
{
    /// <summary>
    /// 基于 SqlSugar 的 <see cref="IChatMessageStore"/> 实现。
    /// 表结构：<c>agent_message(id, conversation_id, role, content, created_at)</c>。
    /// </summary>
    public sealed class MySqlChatMessageStore : IChatMessageStore
    {
        private readonly ISqlSugarClient _db;

        public MySqlChatMessageStore(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<StoredChatMessage>>
            LoadRecentAsync(
                string conversationId,
                int count,
                CancellationToken cancellationToken)
        {
            // 先按 id DESC 取最新 N 条，再反转成时间正序。
            var entities = await _db.Queryable<AgentMessageEntity>()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.Id, OrderByType.Desc)
                .Take(count)
                .ToListAsync(cancellationToken);

            entities.Reverse();

            return entities
                .Select(e => new StoredChatMessage(e.Role, e.Content, e.Id))
                .ToList();
        }

        public async Task AppendAsync(
            string conversationId,
            IReadOnlyList<StoredChatMessage> messages,
            CancellationToken cancellationToken)
        {
            if (messages.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var entities = messages
                .Select(m => new AgentMessageEntity
                {
                    ConversationId = conversationId,
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAt = now
                })
                .ToList();

            try
            {
                // SqlSugar 的事务 API 不接受 CancellationToken。
                // 在调用前显式检查，避免在拿到事务后再取消。
                cancellationToken.ThrowIfCancellationRequested();

                _db.AsTenant().BeginTran();
                await _db.Insertable(entities)
                    .ExecuteCommandAsync(cancellationToken);
                _db.AsTenant().CommitTran();
            }
            catch
            {
                _db.AsTenant().RollbackTran();
                throw;
            }
        }
    }
}
