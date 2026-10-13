namespace historydemo1.Services
{
    using System.ComponentModel;
    public sealed class DroneAlarmTools
    {
        private readonly IAlarmStatisticsService _statisticsService;
        private readonly TimeProvider _timeProvider;

        public DroneAlarmTools(
            IAlarmStatisticsService statisticsService,
            TimeProvider timeProvider)
        {
            _statisticsService = statisticsService;
            _timeProvider = timeProvider;
        }

        [Description(
            "查询最近指定分钟数内的无人机告警数量。" +
            "只能用于查询统计数据，不用于修改告警记录。")]
        public async Task<AlarmStatisticsResult> GetRecentDroneAlarmStatistics(
            [Description("查询最近多少分钟，范围为1到1440。")]
        int minutes,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("查询告警数量方法被调用了");
            if (minutes < 1 || minutes > 1440)
            {
                return new AlarmStatisticsResult
                {
                    Success = false,
                    Error = "查询时间范围必须为1到1440分钟。"
                };
            }

            var endTime = _timeProvider.GetUtcNow();
            var startTime = endTime.AddMinutes(-minutes);

            var count = await _statisticsService.CountRecentAlarmsAsync(
                startTime,
                endTime,
                cancellationToken);
            Console.WriteLine("查询告警数量方法被调用了，结果：{0}", count);
            return new AlarmStatisticsResult
            {
                Success = true,
                Minutes = minutes,
                TotalAlarmCount = count,
                QueryTime = endTime
            };
        }
    }
}
