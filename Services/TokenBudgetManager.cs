using historydemo1.Models;
using historydemo1.Services;
using Microsoft.Extensions.AI;

public sealed class TokenBudgetManager
{
    private readonly ITokenEstimator _estimator;
    private readonly IChatMessageStore _store;

    public TokenBudgetManager(ITokenEstimator estimator,IChatMessageStore store)
    {
        _estimator = estimator;                                                 
        _store = store;
    }

    public async Task<List<ChatMessage>> LoadRecentWithinBudgetAsync(
       string conversationId,
       long afterMessageId,
       int tokenBudget,
       int pageSize = 30,
       CancellationToken cancellationToken = default)
    {
        var selected = new List<ChatMessage>();
        var usedTokens = 0;

        // 0 表示从最新消息开始。
        long beforeMessageId = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _store.LoadBeforeAsync(
                conversationId,
                afterMessageId,
                beforeMessageId,
                pageSize,
                cancellationToken);

            if (page.Count == 0)
                break;

            // 页面是从旧到新，选择时需要从新到旧遍历。
            for (var i = page.Count - 1; i >= 0; i--)
            {
                var entity = page[i];

                var role = entity.Role switch
                {
                    "user" => ChatRole.User,
                    "assistant" => ChatRole.Assistant,
                    _ => (ChatRole?)null
                };

                // 第一版只处理普通文本用户/助手消息。
                // 工具调用等角色应在后续版本完整支持。
                if (role is null)
                    continue;

                var message = new ChatMessage(
                    role.Value,
                    entity.Content);

                var cost = _estimator.Estimate(message);

                if (usedTokens + cost > tokenBudget)
                {
                    // 不再向更早的历史扩展。
                    selected.Reverse();
                    return selected;
                }

                selected.Add(message);
                usedTokens += cost;
            }

            // 下一页从本页最早的消息之前开始。
            beforeMessageId = page[0].Id;

            if (page.Count < pageSize)
                break;
        }

        selected.Reverse();
        return selected;
    }


    public List<ChatMessage> SelectRecentMessages(
        IReadOnlyList<ChatMessage> chronologicalMessages,
        int tokenBudget)
    {
        if (tokenBudget <= 0)
            return [];

        var selected = new List<ChatMessage>();
        var usedTokens = 0;

        // chronologicalMessages 按时间从旧到新排列。
        for (var i = chronologicalMessages.Count - 1; i >= 0; i--)
        {
            var message = chronologicalMessages[i];
            var cost = _estimator.Estimate(message);

            // 单条消息已超过剩余预算，不再向更早历史扩展。
            if (usedTokens + cost > tokenBudget)
                break;

            selected.Add(message);
            usedTokens += cost;
        }

        // 恢复从旧到新的顺序。
        selected.Reverse();

        return selected;
    }
}
