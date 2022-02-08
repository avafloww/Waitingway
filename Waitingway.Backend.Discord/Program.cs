using System.Net;
using System.Security.Claims;
using CacheTower;
using CacheTower.Extensions;
using CacheTower.Providers.Memory;
using CacheTower.Providers.Redis;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Waitingway.Backend.Database;
using Waitingway.Backend.Database.Models;
using Waitingway.Backend.Discord;
using Waitingway.Backend.Discord.Queue;
using Waitingway.Backend.Discord.Worker;
using DiscordConfig = Waitingway.Backend.Discord.DiscordConfig;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // trust forwarding from Docker
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
});

// web shit
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Web/Views/{0}.cshtml");
});

// postgres/redis
builder.Services.AddDbContext<WaitingwayContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pg")));

var redis = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("redis"));
if (redis == null)
{
    throw new Exception("Redis connection failed");
}

builder.Services.AddSingleton<ConnectionMultiplexer>(_ => redis);

// caching
builder.Services.AddCacheStack<DiscordLinkInfo>(
    new ICacheLayer[]
    {
        new MemoryCacheLayer(),
        new RedisCacheLayer(redis)
    },
    new ICacheExtension[]
    {
        new AutoCleanupExtension(TimeSpan.FromMinutes(30))
    }
);

builder.Services.AddCacheStack<DiscordQueueMessage>(
    new ICacheLayer[]
    {
        new MemoryCacheLayer(),
        new RedisCacheLayer(redis)
    },
    new ICacheExtension[]
    {
        new AutoCleanupExtension(TimeSpan.FromMinutes(30))
    }
);

// spin up the config
var discordConfig = new DiscordConfig();
builder.Configuration.GetSection("Discord").Bind(discordConfig);
builder.Services.AddSingleton(discordConfig);

// add a Discord socket client singleton
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<DiscordMainWorker>();
builder.Services.AddSingleton<QueueListenerService>();
builder.Services.AddSingleton<DiscordService>();
builder.Services.AddHostedService(p => p.GetRequiredService<DiscordMainWorker>());
builder.Services.AddHostedService(p => p.GetRequiredService<QueueListenerService>());

// OAuth support
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddDiscord(options =>
    {
        options.CallbackPath = "/callback";
        options.AppId = discordConfig.ClientId;
        options.AppSecret = discordConfig.ClientSecret;
        options.Scope.Add("identify");
        options.Scope.Add("guilds.join");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var idString = context.Identity?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
                if (idString == null || context.AccessToken == null)
                {
                    return;
                }

                var id = ulong.Parse(idString);
                var ds = context.HttpContext.RequestServices.GetRequiredService<DiscordService>();
                await ds.JoinUserToGuild(id, context.AccessToken);
            }
        };
    });
builder.Services.AddAuthorization();

// let's go
var app = builder.Build();
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();
await app.RunAsync();
