namespace historydemo1.Models
{
    using SqlSugar;

    [SugarTable("agent_conversation_summary")]
    public sealed class ConversationSummaryEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        public string ConversationId { get; set; } = "";
        public string UserId { get; set; } = "";

        [SugarColumn(ColumnDataType = "longtext")]
        public string Summary { get; set; } = "";

        public long SummarizedUntilMessageId { get; set; }

        public int Version { get; set; } = 1;

        public DateTime UpdatedAt { get; set; }
    }
}
