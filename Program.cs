
using historydemo1.Models;
using historydemo1.Models.Options;
using historydemo1.Services;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using SqlSugar;
using StackExchange.Redis;
using System.ClientModel;

namespace historydemo1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<AIOptions>(builder.Configuration.GetSection(AIOptions.SectionName));

            // Add services to the container.
            builder.Services.AddSingleton(sp =>
            {
                var aiOptionsOptions = sp.GetService<IOptions<AIOptions>>();
                var aiOptions = aiOptionsOptions.Value;
                var openclient = new OpenAIClient(new ApiKeyCredential(aiOptions.ApiKey), new OpenAIClientOptions
                {
                    Endpoint = new Uri(aiOptions.Endpoint)
                });
                var chatClient = openclient.GetChatClient(aiOptions.ModelId).AsIChatClient();
                return chatClient;
            });

            builder.Services.AddSingleton<DroneAlarmTools>();
            builder.Services.AddSingleton<IAlarmStatisticsService, AlarmStatisticsService>();
            builder.Services.AddSingleton<IConversationSummaryRepository, SqlSugarConversationSummaryRepository>();
            builder.Services.AddSingleton<IConversationSummaryService, ConversationSummaryService>();
            builder.Services.AddSingleton<DatabaseChatHistoryProvider>();
            builder.Services.AddSingleton<AIAgent>(sp =>
            {
                var chatClient = sp.GetService<IChatClient>();
                var provider = sp.GetRequiredService<DatabaseChatHistoryProvider>();

                var tool = AIFunctionFactory.Create(
     typeof(DroneAlarmTools).GetMethod(
         nameof(DroneAlarmTools.GetRecentDroneAlarmStatistics))!,
     target: sp.GetRequiredService<DroneAlarmTools>());
                var agent= chatClient.AsAIAgent(new ChatClientAgentOptions
               {
                    ChatOptions=new ChatOptions
                    {
                         Instructions= """
                         你是一个 .NET 技术助手。
                         请用中文回答问题。
                         请结合当前对话上下文回答用户问题。
                         如果不知道答案，不要编造。
                         """                     ,
                        Tools = [tool]
                    },
                    ChatHistoryProvider= provider
               },services:sp);
                return agent;
            });
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var connectionString =
                    builder.Configuration["Redis:ConnectionString"]
                    ?? throw new InvalidOperationException(
                        "缺少 Redis:ConnectionString");

                return ConnectionMultiplexer.Connect(connectionString);
            });
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddScoped<TokenBudgetManager>();
            builder.Services.AddSingleton<ITokenEstimator,ApproximateTokenEstimator>();
            builder.Services.AddSingleton<RedisAgentSessionRepository>();

            builder.Services.AddSingleton<ISqlSugarClient>(_ =>
            {
                var connectionString =
                    builder.Configuration.GetConnectionString("AgentDb")
                    ?? throw new InvalidOperationException(
                        "缺少 ConnectionStrings:AgentDb");

                return new SqlSugarScope(new ConnectionConfig
                {
                    ConnectionString = connectionString,
                    DbType = DbType.MySql,
                    IsAutoCloseConnection = true
                });
            });

            builder.Services.AddSingleton<IChatMessageStore, MySqlChatMessageStore>();

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            var  db = app.Services.GetRequiredService<ISqlSugarClient>();
            db.DbMaintenance.CreateDatabase();
            db.CodeFirst.InitTables<AgentMessageEntity,ConversationSummaryEntity>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
