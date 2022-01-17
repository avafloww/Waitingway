namespace Waitingway.Backend.Server.Client;

public class Client
{
    public string Id { get; init; }
    private string _pluginVersion;

    public string PluginVersion
    {
        get => _pluginVersion;
        init
        {
            // 4-component versions need to go die in a fire
            var split = value.Split(".");
            _pluginVersion = split.Length == 4 ? string.Join(".", split.SkipLast(1)) : value;
        }
    }
}