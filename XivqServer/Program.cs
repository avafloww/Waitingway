using XIVq.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddScoped<XivqHub>();
builder.Services.AddScoped<ClientManager>();
var app = builder.Build();

app.MapHub<XivqHub>("/ws");

app.Run();