using Microsoft.EntityFrameworkCore;
using Waitingway.Backend.Analysis;
using Waitingway.Backend.Database;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddDbContext<WaitingwayContext>(options =>
            options.UseNpgsql(ctx.Configuration.GetConnectionString("pg")));
        services.AddScoped<AnalysisService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();