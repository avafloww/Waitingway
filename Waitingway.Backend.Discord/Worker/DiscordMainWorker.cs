using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Waitingway.Backend.Discord.Worker;

public class DiscordMainWorker : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordMainWorker> _logger;
    private readonly DiscordConfig _config;

    public DiscordMainWorker(DiscordSocketClient client, ILogger<DiscordMainWorker> logger, DiscordConfig config)
    {
        _client = client;
        _logger = logger;
        _config = config;

        _client.Log += LogMessage;
    }

    internal async Task JoinUserToGuild(ulong userId, string accessToken)
    {
        await _client.Rest.AddGuildUserAsync(_config.GuildId, userId, accessToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.LoginAsync(TokenType.Bot, _config.BotToken);
        await _client.StartAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }

        await _client.StopAsync();
    }

    private Task LogMessage(LogMessage message)
    {
        // convert the log message into an ILogger message
        LogLevel level;
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                level = LogLevel.Critical;
                break;
            case LogSeverity.Error:
                level = LogLevel.Error;
                break;
            case LogSeverity.Warning:
                level = LogLevel.Warning;
                break;
            case LogSeverity.Info:
            default:
                level = LogLevel.Information;
                break;
            case LogSeverity.Verbose:
                level = LogLevel.Trace;
                break;
            case LogSeverity.Debug:
                level = LogLevel.Debug;
                break;
        }

        if (message.Exception != null)
        {
            _logger.Log(level, message.Exception, "[{}] {}", message.Source, message.Message);
        }
        else
        {
            _logger.Log(level, "[{}] {}", message.Source, message.Message);
        }

        return Task.CompletedTask;
    }
}