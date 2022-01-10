using System;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace Waitingway.Dalamud;

public class Configuration : IPluginConfiguration
{
    public string RemoteServer { get; } = "https://etheirys.waitingway.com";
    public string ClientId { get; } = Guid.NewGuid().ToString();
    public int Version { get; set; }

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