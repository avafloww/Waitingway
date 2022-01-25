using Microsoft.EntityFrameworkCore;
using Waitingway.Backend.Database;

namespace Waitingway.Backend.Analysis.Database;

public class AnalysisContext : WaitingwayContext
{
    public DbSet<QueueSessionAnalysisData> PastQueueSessions { get; set; }

    public AnalysisContext(DbContextOptions options) : base(options)
    {
    }
}