using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Waitingway.Backend.Database;

public class WaitingwayContextFactory : IDesignTimeDbContextFactory<WaitingwayContext>
{
    public WaitingwayContext CreateDbContext(string[] args)
    {
        var ob = new DbContextOptionsBuilder<WaitingwayContext>();
        ob.UseNpgsql("Host=localhost;Username=waitingway_dev;Password=dev;Database=waitingway_dev");

        return new WaitingwayContext(ob.Options);
    }
}