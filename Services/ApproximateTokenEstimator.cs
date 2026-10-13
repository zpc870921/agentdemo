using Microsoft.Extensions.AI;

namespace historydemo1.Services
{
    public sealed class ApproximateTokenEstimator : ITokenEstimator
    {
        public int Estimate(ChatMessage message)
        {
            var text = message.Text ?? "";

            // 仅用于演示的粗略估算：
            // 中文、英文、标点、代码的实际 Token 比例不同。
            return Math.Max(1, (int)Math.Ceiling(text.Length / 2.0));
        }
    }
}
