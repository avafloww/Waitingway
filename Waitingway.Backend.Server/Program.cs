using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Waitingway.Backend.Database;
using Waitingway.Backend.Server;
using Waitingway.Backend.Server.Client;
using Waitingway.Backend.Server.Queue;

var builder = WebApplication.CreateBuilder(args);

// websockets
builder.Services.AddSignalR();

// redis
builder.Services.AddSingleton<ConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("redis")));
builder.Services.AddScoped<WaitingwayHub>();

// managers
builder.Services.AddScoped<QueueManager>();
builder.Services.AddScoped<ClientManager>();

// reaper
builder.Services.AddHostedService<ClientReaperService>();

// estimates
builder.Services.AddSingleton<QueueListenerService>();
builder.Services.AddHostedService(p => p.GetRequiredService<QueueListenerService>());

// db
builder.Services.AddDbContext<WaitingwayContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pg")));

// map the ws hub
var app = builder.Build();
app.MapHub<WaitingwayHub>("");

// restore what was here when we last exited
await using (var scope = app.Services.CreateAsyncScope())
{
    // restore existing clients before we start accepting connections
    await scope.ServiceProvider.GetRequiredService<ClientManager>().RestoreFromDb();
}

// let's go
await app.RunAsync();