using Microsoft.Extensions.AI;

namespace historydemo1.Services
{
    public interface ITokenEstimator
    {
        int Estimate(ChatMessage message);
    }
}


