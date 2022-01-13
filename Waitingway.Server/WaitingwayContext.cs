using Microsoft.EntityFrameworkCore;
using Waitingway.Server.Models;

namespace Waitingway.Server;

public class WaitingwayContext : DbContext
{
    public DbSet<QueueSession> QueueSessions { get; set; }
    public DbSet<QueueSessionData> QueueSessionData { get; set; }
    public DbSet<RecentlyActiveQueueSession> RecentlyActiveQueueSessions { get; set; }

    public WaitingwayContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<RecentlyActiveQueueSession>()
            .ToView("RecentlyActiveQueueSessions")
            .HasKey(t => t.Id);
    }
}