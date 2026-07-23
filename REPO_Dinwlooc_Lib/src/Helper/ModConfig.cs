using BepInEx.Configuration;

namespace Dinwlooc.Common.Helpers;

public abstract class ModConfig<T> where T : ModConfig<T>, new()
{
    private static T? _instance;
    public static T Instance => _instance ??= new T();

    protected ModConfig() { }

    public virtual ConfigEntry<bool>? Enabled { get; protected set; }
    public virtual ConfigEntry<int>? PosX { get; protected set; }
    public virtual ConfigEntry<int>? PosY { get; protected set; }

    protected abstract void Bind(ConfigFile config);

    public void Initialize(ConfigFile config)
    {
        Bind(config);
        if (Enabled == null)
            Enabled = config.Bind("General", "Enabled", true, "是否启用该模组");
        if (PosX == null)
            PosX = config.Bind("UI", "PosX", 200, "按钮 X 偏移");
        if (PosY == null)
            PosY = config.Bind("UI", "PosY", 100, "按钮 Y 偏移");
    }
}