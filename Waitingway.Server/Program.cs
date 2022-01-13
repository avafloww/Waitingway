using Microsoft.EntityFrameworkCore;
using Waitingway.Server;
using Waitingway.Server.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddScoped<WaitingwayHub>();
builder.Services.AddScoped<ClientManager>();
builder.Services.AddHostedService<ClientReaperService>();
builder.Services.AddDbContext<WaitingwayContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pg")));
var app = builder.Build();
app.MapHub<WaitingwayHub>("");

await using (var scope = app.Services.CreateAsyncScope())
{
    // restore existing clients before we start accepting connections
    // todo: enable when this is ready
    // await scope.ServiceProvider.GetRequiredService<ClientManager>().Restore();
}

await app.RunAsync();
