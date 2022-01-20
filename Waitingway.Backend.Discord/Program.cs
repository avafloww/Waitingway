using System.Security.Claims;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Waitingway.Backend.Database;
using Waitingway.Backend.Discord.Worker;
using DiscordConfig = Waitingway.Backend.Discord.DiscordConfig;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// web shit
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Web/Views/{0}.cshtml");
});

// postgres
builder.Services.AddDbContext<WaitingwayContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("pg")));

// spin up the config
var discordConfig = new DiscordConfig();
builder.Configuration.GetSection("Discord").Bind(discordConfig);
builder.Services.AddSingleton(discordConfig);

// add a Discord socket client singleton
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<DiscordMainWorker>();
builder.Services.AddHostedService(p => p.GetRequiredService<DiscordMainWorker>());

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
                await context.HttpContext.RequestServices.GetRequiredService<DiscordMainWorker>()
                    .JoinUserToGuild(id, context.AccessToken);
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