using Microsoft.EntityFrameworkCore;
using Waitingway.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddScoped<WaitingwayHub>();
builder.Services.AddScoped<ClientManager>();
builder.Services.AddDbContext<WaitingwayContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("pg")));
var app = builder.Build();

app.MapHub<WaitingwayHub>("");

app.Run();