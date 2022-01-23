using Waitingway.Backend.Database;

namespace Waitingway.Backend.Analysis;

public class AnalysisService
{
    private readonly ILogger<AnalysisService> _logger;
    private readonly WaitingwayContext _db;

    public AnalysisService(ILogger<AnalysisService> logger, WaitingwayContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Analyse()
    {
        var q = from s in _db.QueueSessions
            join d in _db.QueueSessionData on s.Id equals d.Session.Id into composed
            from sd in composed
            where sd.Time >= DateTime.Now - TimeSpan.FromDays(7)
            select new {s.Id, sd.Time, s.SessionType};

        foreach (var sd in q)
        {
            _logger.LogInformation("Id: {} / Time: {} / Type: {}", sd.Id, sd.Time, sd.SessionType);
        }
    }
}