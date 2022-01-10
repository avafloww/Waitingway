using Waitingway.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddScoped<WaitingwayHub>();
builder.Services.AddScoped<ClientManager>();
var app = builder.Build();

app.MapHub<WaitingwayHub>("/ws");

app.Run();