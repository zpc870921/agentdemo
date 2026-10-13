using System.Text.Json;

namespace historydemo1.Models
{
    public sealed class StoredMessageSerializer : IStoredMessageSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public string Serialize(StoredAgentMessage message)
        {
            return JsonSerializer.Serialize(message, Options);
        }

        public StoredAgentMessage Deserialize(string json)
        {
            return JsonSerializer.Deserialize<StoredAgentMessage>(
                       json, Options)
                   ?? throw new InvalidOperationException(
                       "无法解析持久化消息");
        }
    }
}
