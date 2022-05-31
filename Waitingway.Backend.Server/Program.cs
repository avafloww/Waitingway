using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Waitingway.Backend.Database;
using Waitingway.Backend.Server;
using Waitingway.Backend.Server.Client;
using Waitingway.Backend.Server.Queue;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<ConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("redis")));
builder.Services.AddScoped<WaitingwayHub>();
builder.Services.AddScoped<QueueManager>();
builder.Services.AddScoped<ClientManager>();
builder.Services.AddHostedService<ClientReaperService>();
builder.Services.AddDbContext<WaitingwayContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pg")));
var app = builder.Build();
app.MapHub<WaitingwayHub>("");

await using (var scope = app.Services.CreateAsyncScope())
{
    // restore existing clients before we start accepting connections
    scope.ServiceProvider.GetRequiredService<ClientManager>().Restore();
}

await app.RunAsync();
