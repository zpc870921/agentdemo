namespace historydemo1.Services
{

    using historydemo1.Models;
    using Microsoft.Agents.AI;
    using Microsoft.Extensions.AI;
    using System.Text.Json.Serialization;

    public sealed class DatabaseChatHistoryProvider
        : ChatHistoryProvider
    {
        private readonly IChatMessageStore _store;

        private readonly ProviderSessionState<State> _sessionState;

        // 每次模型调用暂时最多加载最近 20 条文本消息。
        // 下一阶段会将它升级为 Token 预算控制。
        private const int RecentMessageCount = 20;

        public DatabaseChatHistoryProvider(IChatMessageStore store)
        {
            _store = store;

            _sessionState = new ProviderSessionState<State>(
                stateInitializer: _ => new State(),
                stateKey: nameof(DatabaseChatHistoryProvider));
        }

        //public override string StateKey => _sessionState.StateKey;
        public override IReadOnlyList<string> StateKeys => [_sessionState.StateKey];

        // 由 WebAPI 在调用 Agent 前设置会话 ID。
        public void SetConversationId(
            AgentSession session,
            string conversationId)
        {
            var state = _sessionState.GetOrInitializeState(session);

            state.ConversationId = conversationId;

            _sessionState.SaveState(session, state);
        }

        private string GetConversationId(AgentSession? session)
        {
            if (session is null)
            {
                throw new InvalidOperationException(
                    "数据库历史 Provider 需要 AgentSession。");
            }

            var state = _sessionState.GetOrInitializeState(session);

            if (string.IsNullOrWhiteSpace(state.ConversationId))
            {
                throw new InvalidOperationException(
                    "Session 尚未设置 ConversationId。");
            }

            return state.ConversationId;
        }

        // 1. 调用模型之前：从数据库加载历史。
        protected override async ValueTask<IEnumerable<ChatMessage>>
            ProvideChatHistoryAsync(
                InvokingContext context,
                CancellationToken cancellationToken = default)
        {
            var conversationId = GetConversationId(context.Session);

            var rows = await _store.LoadRecentAsync(
                conversationId,
                RecentMessageCount,
                cancellationToken);

            var messages = rows.Select(row =>
                new ChatMessage(
                    new ChatRole(row.Role),
                    row.Content));

            return messages;
        }

        // 2. 模型调用完成之后：只保存本轮新消息。
        protected override async ValueTask StoreChatHistoryAsync(
            InvokedContext context,
            CancellationToken cancellationToken = default)
        {
            var conversationId = GetConversationId(context.Session);

            var newMessages = context.RequestMessages
                .Concat(context.ResponseMessages ?? [])
                .Select(message => new StoredChatMessage(
                    message.Role.Value,
                    string.Join(
                        "",
                        message.Contents
                            .OfType<TextContent>()
                            .Select(content => content.Text))))
                .Where(message => !string.IsNullOrWhiteSpace(message.Content))
                .ToList();

            await _store.AppendAsync(
                conversationId,
                newMessages,
                cancellationToken);
        }

        public sealed class State
        {
            [JsonPropertyName("conversationId")]
            public string ConversationId { get; set; } = "";
        }
    }

}
