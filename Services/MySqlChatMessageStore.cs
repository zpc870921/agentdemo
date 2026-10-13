using historydemo1.Models;
using SqlSugar;

namespace historydemo1.Services
{
    /// <summary>
    /// 基于 SqlSugar 的 <see cref="IChatMessageStore"/> 实现。
    /// 表结构：<c>agent_message(id, conversation_id, role, content, created_at)</c>。
    /// </summary>
    //public sealed class MySqlChatMessageStore : IChatMessageStore
    //{
    //    private readonly ISqlSugarClient _db;

    //    public MySqlChatMessageStore(ISqlSugarClient db)
    //    {
    //        _db = db;
    //    }

    //    public async Task<IReadOnlyList<StoredChatMessage>>
    //        LoadRecentAsync(
    //            string conversationId,
    //            int count,
    //            CancellationToken cancellationToken)
    //    {
    //        // 先按 id DESC 取最新 N 条，再反转成时间正序。
    //        var entities = await _db.Queryable<AgentMessageEntity>()
    //            .Where(m => m.ConversationId == conversationId)
    //            .OrderBy(m => m.Id, OrderByType.Desc)
    //            .Take(count)
    //            .ToListAsync(cancellationToken);

    //        entities.Reverse();

    //        return entities
    //            .Select(e => new StoredChatMessage{ Role = e.Role, Content = e.ContentJson, Id = e.Id })
    //            .ToList();
    //    }

    //    public async Task AppendAsync(
    //        string conversationId,
    //        IReadOnlyList<StoredChatMessage> messages,
    //        CancellationToken cancellationToken)
    //    {
    //        if (messages.Count == 0)
    //        {
    //            return;
    //        }

    //        var now = DateTime.UtcNow;
    //        var entities = messages
    //            .Select(m => new AgentMessageEntity
    //            {
    //                ConversationId = conversationId,
    //                Role = m.Role,
    //                ContentJson = m.Content,
    //                CreatedAt = now
    //            })
    //            .ToList();

    //        try
    //        {
    //            // SqlSugar 的事务 API 不接受 CancellationToken。
    //            // 在调用前显式检查，避免在拿到事务后再取消。
    //            cancellationToken.ThrowIfCancellationRequested();

    //            _db.AsTenant().BeginTran();
    //            await _db.Insertable(entities)
    //                .ExecuteCommandAsync(cancellationToken);
    //            _db.AsTenant().CommitTran();
    //        }
    //        catch
    //        {
    //            _db.AsTenant().RollbackTran();
    //            throw;
    //        }
    //    }

    //    public async Task<List<StoredChatMessage>> LoadAfterAsync(string conversationId, long messageId, CancellationToken cancellationToken)
    //    {
    //        var list = await _db.Queryable<AgentMessageEntity>().Where(x =>x.ConversationId == conversationId && x.Id > messageId)
    //              .OrderBy(x => x.Id, OrderByType.Asc)
    //              .Select(x => new StoredChatMessage { 
    //                Role=x.Role,
    //                Content=x.ContentJson,
    //                Id=x.Id
    //              })
    //              .ToListAsync();


    //        return list;
    //    }

    //    public async Task<List<AgentMessageEntity>> LoadBeforeAsync(string conversationId, long afterMessageId, long beforeMessageId, int pageSize, CancellationToken cancellationToken)
    //    {
    //        if (pageSize <= 0)
    //            throw new ArgumentOutOfRangeException(nameof(pageSize));

    //        var query = _db.Queryable<AgentMessageEntity>()
    //            .Where(x => x.ConversationId == conversationId)
    //             .Where(x => x.Id > afterMessageId); 

    //        if (beforeMessageId > 0)
    //        {
    //            query = query.Where(x => x.Id < beforeMessageId);
    //        }

    //        var rows = await query
    //            .OrderBy(x => x.Id, OrderByType.Desc)
    //            .Take(pageSize)
    //            .ToListAsync();

    //        // SQL 返回最新消息在前；组装上下文时恢复正序。
    //        rows.Reverse();

    //        return rows;
    //    }
    //}

    public sealed class MySqlChatMessageStore : IChatMessageStore
    {
        private readonly ISqlSugarClient _db;

        public MySqlChatMessageStore(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task AppendAsync(
            AgentMessageEntity message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message.ConversationId))
                throw new ArgumentException("conversationId 不能为空");

            if (string.IsNullOrWhiteSpace(message.TurnId))
                throw new ArgumentException("turnId 不能为空");

            if (string.IsNullOrWhiteSpace(message.ContentJson))
                throw new ArgumentException("ContentJson 不能为空");

            // 防止非法 JSON 被写入
            using var _ = System.Text.Json.JsonDocument.Parse(
                message.ContentJson);

            if (message.CreatedAt == default)
                message.CreatedAt = DateTime.UtcNow;

            await _db.Insertable(message).ExecuteCommandAsync();
        }

        public async Task<IReadOnlyList<AgentMessageEntity>> LoadAfterAsync(
            string conversationId,
            long afterMessageId,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 500);

            return await _db.Queryable<AgentMessageEntity>()
                .Where(x => x.ConversationId == conversationId)
                .Where(x => x.Id > afterMessageId)
                .OrderBy(x => x.Id)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<AgentMessageEntity>> LoadRecentTurnsAsync(
            string conversationId,
            long afterMessageId,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // 第一版先复用按 ID 查询的逻辑。
            // 下一步再改成完整轮次分页，避免切断工具调用链。
            return await LoadAfterAsync(
                conversationId,
                afterMessageId,
                pageSize,
                cancellationToken);
        }
    }
}
