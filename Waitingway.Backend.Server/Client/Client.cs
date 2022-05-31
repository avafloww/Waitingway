namespace Waitingway.Backend.Server.Client;

public class Client
{
    public static Version LatestPluginVersion = new("1.2.1");
    public static Version OldestSupportedPluginVersion = new("1.2.1");

    public string Id { get; init; }
    private string _pluginVersion;

    public string GameVersion { get; init; }

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

    public bool DiscordLinked { get; set; }

    public bool IsSupportedVersion
    {
        get
        {
            try
            {
                return Version.Parse(PluginVersion) >= OldestSupportedPluginVersion;
            }
            catch
            {
                return true;
            }
        }
    }

    public bool IsLatestVersion
    {
        get
        {
            try
            {
                return Version.Parse(PluginVersion) >= LatestPluginVersion;
            }
            catch
            {
                return true;
            }
        }
    }
}
