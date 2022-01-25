using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using StackExchange.Redis;
using Waitingway.Backend.Analysis;
using Waitingway.Backend.Analysis.Database;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<ConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(ctx.Configuration.GetConnectionString("redis")));

        var modelPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName) +
                        @"\Data\LoginQueueModel.zip";
        services.AddPredictionEnginePool<QueueSessionAnalysisData, QueueTimePrediction>()
            .FromFile("LoginQueueModel", modelPath, true);

        services.AddDbContext<AnalysisContext>(options =>
            options.UseNpgsql(ctx.Configuration.GetConnectionString("pg")));
        services.AddScoped<AnalysisService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();