using historydemo1.Models;
using historydemo1.Services;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;

namespace historydemo1.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class WeatherController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        private readonly AIAgent _agent;
        private readonly RedisAgentSessionRepository _redisAgentSession;
        private static readonly ConcurrentDictionary<string,AgentSession> _sessions = new ConcurrentDictionary<string, AgentSession>();
        public WeatherController(AIAgent agent,RedisAgentSessionRepository redisAgentSessionRepository)
        {
            _agent = agent;
            _redisAgentSession = redisAgentSessionRepository;
        }



        [HttpPost]
        public async Task<IActionResult> Get(ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ConversationId))
                return BadRequest("ConversationId不能为空");
            if(string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message不能为空");

            // var session = _sessions.GetOrAdd(request.ConversationId, _ => _agent.CreateSessionAsync().GetAwaiter().GetResult());
            var session =await _redisAgentSession.GetAsync(_agent, request.ConversationId);
            if (null == session)
                session =await _agent.CreateSessionAsync();

            var response = await _agent.RunAsync(request.Message,session);

            await _redisAgentSession.SaveAsync(_agent, request.ConversationId,session);

            Console.WriteLine("=========history========");
            var messages = _agent.GetService<InMemoryChatHistoryProvider>().GetMessages(session);
            foreach (var item in messages)
            {
                foreach (var content in item.Contents)
                {
                   if(content is TextContent textContent)
                   {
                       Console.WriteLine(textContent.Text);
                   }
                }
            }
            return Ok(new
            {
                request.ConversationId,
                request.Message,
                answer= response.ToString()
            });
        }
    }

    
}
