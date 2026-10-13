using SqlSugar;

namespace historydemo1.Models
{
    /// <summary>
    /// SqlSugar 实体映射到表 <c>agent_message</c>。
    /// 列名通过 <see cref="SugarColumnAttribute"/> 与数据库保持一致。
    /// </summary>
    [SugarTable("agent_message")]
    public sealed class AgentMessageEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "conversation_id")]
        public string ConversationId { get; set; } = "";

        [SugarColumn(ColumnName = "user_id")]
        public string UserId { get; set; } = "";
        
        [SugarColumn(ColumnName = "turn_id")]
        public string TurnId { get; set; } = "";

        [SugarColumn(ColumnName = "sequence_no")]
        public int SequenceNo { get; set; }

        [SugarColumn(ColumnName = "role")]
        public string Role { get; set; } = "";

        [SugarColumn(ColumnName = "message_type")]
        public string MessageType { get; set; } = "text";

        [SugarColumn(ColumnName = "content", ColumnDataType = "text")]
        public string ContentJson { get; set; } = "{}";

        [SugarColumn(ColumnName = "created_at")]
        public DateTime CreatedAt { get; set; }
    }
    //public sealed class AgentMessageEntity
    //{
    //    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    //    public long Id { get; set; }

    //    [SugarColumn(ColumnName = "conversation_id")]
    //    public string ConversationId { get; set; } = string.Empty;

    //    [SugarColumn(ColumnName = "role")]
    //    public string Role { get; set; } = string.Empty;

    //    [SugarColumn(ColumnName = "content",ColumnDataType ="text")]
    //    public string Content { get; set; } = string.Empty;

    //    [SugarColumn(ColumnName = "created_at")]
    //    public DateTime CreatedAt { get; set; }
    //}
}
