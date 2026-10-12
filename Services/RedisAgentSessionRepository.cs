using System.Text.Json;
using Microsoft.Agents.AI;
using StackExchange.Redis;

namespace historydemo1.Services
{



    public sealed class RedisAgentSessionRepository
    {
        private readonly IDatabase _redis;
        private readonly string _keyPrefix;
        private readonly TimeSpan _expiration;

        public RedisAgentSessionRepository(
            IConnectionMultiplexer multiplexer,
            IConfiguration configuration)
        {
            _redis = multiplexer.GetDatabase();

            _keyPrefix =
                configuration["Redis:KeyPrefix"]
                ?? "agent:session:";

            var hours = configuration.GetValue<int>(
                "Redis:ExpirationHours", 168);

            _expiration = TimeSpan.FromHours(hours);
        }

        private string BuildKey(string conversationId)
        {
            return _keyPrefix + conversationId;
        }

        public async Task<AgentSession?> GetAsync(
            AIAgent agent,
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            var key = BuildKey(conversationId);

            var json = await _redis.StringGetAsync(key);

            if (json.IsNullOrEmpty)
            {
                return null;
            }

            using var document =
                JsonDocument.Parse(json.ToString());

            // Clone 后，JsonElement 不再依赖 JsonDocument 的生命周期。
            var serializedSession =
                document.RootElement.Clone();

            return await agent.DeserializeSessionAsync(
                serializedSession,
                cancellationToken: cancellationToken);
        }

        public async Task SaveAsync(
            AIAgent agent,
            string conversationId,
            AgentSession session,
            CancellationToken cancellationToken = default)
        {
            var key = BuildKey(conversationId);

            var serializedSession =
                await agent.SerializeSessionAsync(
                    session,
                    cancellationToken: cancellationToken);

            var json = serializedSession.GetRawText();

            await _redis.StringSetAsync(
                key,
                json,
                expiry: _expiration);
        }

        public Task DeleteAsync(string conversationId)
        {
            return _redis.KeyDeleteAsync(
                BuildKey(conversationId));
        }
    }

}
