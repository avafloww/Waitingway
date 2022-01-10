using System;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace Waitingway.Dalamud;

public class Configuration : IPluginConfiguration
{
    public string RemoteServer { get; init; } = "https://etheirys.waitingway.com";
    public string ClientId { get; init; } = Guid.NewGuid().ToString();
    public string ClientSalt { get; init; } = Guid.NewGuid().ToString().Split('-')[0];
    public int Version { get; set; } = 1;

    [JsonIgnore] private DalamudPluginInterface pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(this);
    }
}