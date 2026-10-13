using historydemo1.Models;
using SqlSugar;

namespace historydemo1.Services
{
    public interface IAlarmStatisticsService
    {
        Task<long> CountRecentAlarmsAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken);
    }


    public class AlarmStatisticsService : IAlarmStatisticsService
    {
        private readonly ISqlSugarClient _db;
        public AlarmStatisticsService(ISqlSugarClient db)
        {
            _db = db;
        }
        public async Task<long> CountRecentAlarmsAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken)
        {
            var ret= await _db.Queryable<AgentMessageEntity>().CountAsync();
            return ret;
        }
    }

    public sealed class AlarmStatisticsResult
    {
        public bool Success { get; set; }

        public int Minutes { get; set; }

        public long TotalAlarmCount { get; set; }

        public DateTimeOffset QueryTime { get; set; }

        public string? Error { get; set; }
    }

}
