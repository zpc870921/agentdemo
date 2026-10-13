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
        private readonly IConversationSummaryRepository  _summaryRepository;
        private readonly TokenBudgetManager _tokenBudgetManager;


        private readonly IConversationSummaryService
            _summaryService;

        // 每次模型调用暂时最多加载最近 20 条文本消息。
        // 下一阶段会将它升级为 Token 预算控制。
        private const int RecentMessageCount = 20;

        public DatabaseChatHistoryProvider(IChatMessageStore store, IConversationSummaryRepository summaryRepository,
        IConversationSummaryService summaryService)
        {
            _store = store;

            _summaryRepository = summaryRepository;
            _summaryService = summaryService;
            //_tokenBudgetManager = tokenBudgetManager;

            _sessionState = new ProviderSessionState<State>(
                stateInitializer: _ => new State(),
                stateKey: nameof(DatabaseChatHistoryProvider));
        }

        //public override string StateKey => _sessionState.StateKey;
        public override IReadOnlyList<string> StateKeys => [_sessionState.StateKey];

        // 由 WebAPI 在调用 Agent 前设置会话 ID。
        public void SetConversationId(
            AgentSession session,
            string conversationId,
             string userId)
        {
            var state = _sessionState.GetOrInitializeState(session);

            state.ConversationId = conversationId;
            state.UserId = userId;

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

        private State GetState(AgentSession session)
        {
            return _sessionState.GetOrInitializeState(session);
        }

        // 1. 调用模型之前：从数据库加载历史。
        protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
                InvokingContext context,
                CancellationToken cancellationToken = default)
        {
            //var conversationId = GetConversationId(context.Session);
            var state =GetState(context.Session);
            var history = new List<ChatMessage>();
            //----------------------------------
            // 1. 加载摘要
            //----------------------------------
            var summary =await _summaryRepository.GetAsync(state.ConversationId,cancellationToken);
            if (summary != null &&
               !string.IsNullOrWhiteSpace(summary.Summary))
            {
                history.Add(
                    new ChatMessage(
                        ChatRole.System,
                        """
                    以下内容是历史对话摘要，
                    仅用于理解上下文，
                    不代表当前实时事实：
                    """ +
                        summary.Summary));
            }       
            //----------------------------------
            // 2. 加载摘要之后的新消息
            //----------------------------------
            var afterId = summary?.SummarizedUntilMessageId ?? 0;
            var messages = await _store.LoadAfterAsync(state.ConversationId, afterId, cancellationToken);
            foreach (var msg in messages)
            {
                history.Add(new ChatMessage(new ChatRole(msg.Role), msg.Content));
            }
            //// 2. 只读取摘要水位之后、符合 Token 预算的近期消息
            //var afterMessageId = summary?.SummarizedUntilMessageId ?? 0;
            //var recentMessages = await _tokenBudgetManager.LoadRecentWithinBudgetAsync(
            //        state.ConversationId,
            //        afterMessageId,
            //        tokenBudget: 6_000,
            //        pageSize: 30,
            //        cancellationToken);
            //history.AddRange(recentMessages);
            return history;
            //var rows = await _store.LoadRecentAsync(
            //    conversationId,
            //    RecentMessageCount,
            //    cancellationToken);

            //var messages = rows.Select(row =>
            //    new ChatMessage(
            //        new ChatRole(row.Role),
            //        row.Content));

            //return messages;
        }

        // 2. 模型调用完成之后：只保存本轮新消息。
        protected override async ValueTask StoreChatHistoryAsync(
            InvokedContext context,
            CancellationToken cancellationToken = default)
         {
            //var conversationId = GetConversationId(context.Session);

            //var newMessages = context.RequestMessages
            //    .Concat(context.ResponseMessages ?? [])
            //    .Select(message => new StoredChatMessage(
            //        message.Role.Value,
            //        string.Join(
            //            "",
            //            message.Contents
            //                .OfType<TextContent>()
            //                .Select(content => content.Text))))
            //    .Where(message => !string.IsNullOrWhiteSpace(message.Content))
            //    .ToList();

            //await _store.AppendAsync(
            //    conversationId,
            //    newMessages,
            //    cancellationToken);

            if (context.InvokeException != null)
            {
                return;
            }

            var state =GetState(context.Session);
            var messages = context.RequestMessages.Concat(context.ResponseMessages ?? []).ToList();
            //----------------------------------
            // 保存原始消息
            //----------------------------------
            await _store.AppendAsync(state.ConversationId,//state.UserId,
                messages.Select(x=>new StoredChatMessage{ Role = x.Role.Value, Content = string.Join(",", x.Contents.OfType<TextContent>().Select(c => c.Text)), Id = 0 }).ToList(),cancellationToken);
            //----------------------------------
            // 判断是否需要摘要
            //----------------------------------

            await _summaryService
                .TryUpdateSummaryAsync(
                    state.ConversationId,
                    state.UserId,
                    cancellationToken);
        }

        public sealed class State
        {
            [JsonPropertyName("conversationId")]
            public string ConversationId { get; set; } = "";
            public string UserId { get; set; }    = "";
        }
    }

}
