using Dalamud.Configuration;

namespace JiaTools;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;

    public float Opacity { get; set; } = 0.85f;
    public float Range { get; set; } = 30f;
    public float FontScale { get; set; } = 1.0f;
    public int MaxObjects { get; set; } = 10;
    public float MergeDistance { get; set; } = 50f;

    public bool ShowPlayers { get; set; } = true;
    public bool ShowLocalPlayer { get; set; }
    public bool ShowBattleNpcs { get; set; } = true;
    public bool ShowEventNpcs { get; set; }
    public bool ShowEventObjs { get; set; }

    public bool ShowEntityID { get; set; } = true;
    public bool ShowDataID { get; set; } = true;
    public bool UseHexID { get; set; }
    public bool ShowPosition { get; set; } = true;
    public bool ShowRotation { get; set; }
    public bool ShowDistance { get; set; } = true;
    public bool ShowCastInfo { get; set; } = true;
    public bool ShowStatusList { get; set; } = true;
    public bool ShowHealth { get; set; } = true;
    public bool ShowMana { get; set; } = true;
    public bool ShowMarker { get; set; } = true;

    public void Save()
    {
        if (DService.PI == null)
        {
            DService.Log?.Error("Cannot save configuration: Plugin interface not initialized");
            return;
        }
        DService.PI.SavePluginConfig(this);
    }
}
