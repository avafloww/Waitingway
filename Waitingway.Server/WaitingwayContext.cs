using Microsoft.EntityFrameworkCore;
using Waitingway.Server.Models;

namespace Waitingway.Server;

public class WaitingwayContext : DbContext
{
    public DbSet<QueueSession> QueueSessions { get; set; }
    public DbSet<QueueSessionData> QueueSessionData { get; set; }

    public WaitingwayContext(DbContextOptions options) : base(options)
    {
    }
}