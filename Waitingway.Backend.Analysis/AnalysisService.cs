using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using StackExchange.Redis;
using Waitingway.Backend.Analysis.Database;
using Waitingway.Backend.Database.Queue;

namespace Waitingway.Backend.Analysis;

public class AnalysisService
{
    private readonly ILogger<AnalysisService> _logger;
    private readonly AnalysisContext _db;
    private readonly ConnectionMultiplexer _redis;
    private readonly PredictionEnginePool<QueueSessionAnalysisData, QueueTimePrediction> _predictionEnginePool;

    public AnalysisService(ILogger<AnalysisService> logger, AnalysisContext db, ConnectionMultiplexer redis,
        PredictionEnginePool<QueueSessionAnalysisData, QueueTimePrediction> predictionEnginePool)
    {
        _logger = logger;
        _db = db;
        _redis = redis;
        _predictionEnginePool = predictionEnginePool;
    }

    public async Task AnalyseAll()
    {
        _logger.LogInformation("Starting analysis of all client queues");

        var rc = _redis.GetDatabase();
        var count = 0;
        foreach (var clientId in rc.SetMembers("clients:queued"))
        {
            try
            {
                await Analyse(clientId);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during analysis for client {}", clientId);
            }
        }

        _logger.LogInformation("Successfully analysed and updated queue times for {} clients", count);
    }

    public Task Analyse(string clientId)
    {
        var rc = _redis.GetDatabase();
        var jsonData = rc.StringGet($"client:{clientId}:queue");
        if (jsonData == RedisValue.Null || jsonData == RedisValue.EmptyString)
        {
            _logger.LogWarning("Queue data key for client {} was missing or empty, skipping", clientId);
            return Task.CompletedTask;
        }

        var queue = ClientQueue.FromJson(jsonData);
        if (queue == null)
        {
            _logger.LogWarning("Queue data for client {} was invalid, skipping", clientId);
            return Task.CompletedTask;
        }

        var input = QueueSessionAnalysisData.From(queue);
        var prediction = _predictionEnginePool.Predict("LoginQueueModel", input);
        _logger.LogInformation("[{}] current position: {} / predicted ETA: {}", clientId, queue.QueuePosition,
            prediction.TimeEstimate);

        var estimate = new QueueEstimate
        {
            ClientId = Guid.Parse(clientId),
            HasEstimate = true,
            Estimate = prediction.TimeEstimate.Duration() // todo: model sometimes returns negative times, investigate
        };

        rc.Publish("queue:estimate", estimate.ToJson());
        return Task.CompletedTask;
    }

    public IEnumerable<QueueSessionAnalysisData> LoadPastSessions()
    {
        return _db.PastQueueSessions.FromSqlRaw(@"
            with data as (
                select s.""Id"",
                       s.""SessionType"",
                       s.""DataCenter"",
                       s.""World"",
                       d.""Time"",
                       d.""Type"",
                       d.""EndReason"",
                       d.""QueuePosition""
                from ""QueueSessions"" s,
                     ""QueueSessionData"" d
                where s.""Id"" = d.""SessionId"")
            select distinct on (data.""Id"") data.""Id"",
                                           data.""SessionType"",
                                           data.""DataCenter"",
                                           data.""World"",
                                           first.""StartTime"",
                                           last.""EndTime"",
                                           last.""EndReason"",
                                           pos.""StartPos""
            from (select distinct on (""Id"") ""Id"" as ""StartId"", ""Time"" as ""StartTime""
                  from data
                  where ""Type"" = 0) first,
                 (select distinct on (""Id"") ""Id"" as ""EndId"", ""Time"" as ""EndTime"", ""EndReason""
                  from data
                  where ""Type"" = 2) last,
                 (select distinct on (""Id"") ""Id"" as ""PosId"", ""QueuePosition"" as ""StartPos""
                  from data
                  where ""Type"" = 1
                    and ""QueuePosition"" < 4294967295
                  order by ""Id"") pos,
                 data
            where ""Id"" = ""StartId""
              and ""Id"" = ""EndId""
              and ""Id"" = ""PosId""
              and last.""EndReason"" != 3;"
        ).ToList();
    }
}