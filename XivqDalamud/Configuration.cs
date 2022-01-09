using System;
using Dalamud.Configuration;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace XIVq.Dalamud;

public class Configuration : IPluginConfiguration
{
    public string ClientId { get; set; }
    public int Version { get; set; }

    [JsonIgnore] private DalamudPluginInterface pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        if (ClientId == null)
        {
            ClientId = Guid.NewGuid().ToString();
            PluginLog.Log($"Generated new ClientId: {ClientId}");
        }
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(this);
    }
}