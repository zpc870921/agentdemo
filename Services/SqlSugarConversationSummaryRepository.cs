namespace historydemo1.Services
{
    using historydemo1.Models;
    using SqlSugar;
    public sealed class SqlSugarConversationSummaryRepository
        : IConversationSummaryRepository
    {
        private readonly ISqlSugarClient _db;
        public SqlSugarConversationSummaryRepository(
            ISqlSugarClient db)
        {
            _db = db;
        }

        /// <summary>
        /// 查询当前摘要
        /// </summary>
        public async Task<ConversationSummaryEntity?> GetAsync(
            string conversationId,
            CancellationToken cancellationToken)
        {
            return await _db
                .Queryable<ConversationSummaryEntity>()
                .Where(x =>
                    x.ConversationId == conversationId)
                .FirstAsync();
        }
        ///// <summary>
        ///// 查询摘要之后的新消息
        ///// </summary>
        //public async Task<List<SummaryInputMessage>> GetMessagesAfterAsync(string conversationId, long messageId, CancellationToken cancellationToken)
        //{

        //    var list =
        //        await _db
        //        .Queryable<AgentMessageEntity>()
        //        .Where(x =>
        //            x.ConversationId == conversationId && x.Id > messageId)
        //        .OrderBy(x => x.Id, OrderByType.Asc)
        //        .Select(x => new SummaryInputMessage(x.Id, x.Role, x.Content))
        //        .ToListAsync();


        //    return list;
        //}
        /// <summary>
        /// 乐观锁保存摘要
        /// </summary>
        public async Task<bool> TrySaveAsync(
            ConversationSummaryEntity summary,
            int expectedVersion,
            CancellationToken cancellationToken)
        {
            // 第一次生成摘要
            if (expectedVersion == 0)
            {
                var exists = await _db.Queryable<ConversationSummaryEntity>().AnyAsync(x => x.ConversationId == summary.ConversationId);
                if (exists)
                {
                    return false;
                }
                summary.Version = 1;
                await _db.Insertable(summary).ExecuteCommandAsync();
                return true;
            }
            // 更新摘要
            var sql = """
                
                UPDATE agent_conversation_summary SET Summary=@Summary, SummarizedUntilMessageId=@MessageId, Version=Version+1,UpdatedAt=UTC_TIMESTAMP(3) WHERE ConversationId=@ConversationId AND UserId=@UserId AND Version=@Version
                
                """;
                
            var rows =
                await _db.Ado.ExecuteCommandAsync(
                    sql,
                    new SugarParameter(
                        "@Summary",
                        summary.Summary),
                    new SugarParameter(
                        "@MessageId",
                        summary.SummarizedUntilMessageId),
                    new SugarParameter(
                        "@ConversationId",
                        summary.ConversationId),
                    new SugarParameter(
                        "@UserId",
                        summary.UserId),
                    new SugarParameter(
                        "@Version",
                        expectedVersion)
                    );
            return rows == 1;
        }
    }
}
