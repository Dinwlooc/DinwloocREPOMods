using BepInEx.Configuration;

namespace Dinwlooc.Common.Helpers;

/// <summary>
/// 基础配置类，仅提供模组总开关（Enabled）。
/// </summary>
/// <typeparam name="T">派生类自身类型（用于单例）</typeparam>
public abstract class ConfigBase<T> where T : ConfigBase<T>, new()
{
    private static T? _instance;
    public static T Instance => _instance ??= new T();

    protected ConfigBase() { }

    public ConfigEntry<bool> Enabled { get; protected set; } = null!;

    /// <summary>
    /// 绑定配置项，子类可重写以自定义绑定。
    /// </summary>
    public virtual void Bind(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "Enable the mod.");
    }

    /// <summary>
    /// 初始化配置，调用 Bind。
    /// </summary>
    public void Initialize(ConfigFile config)
    {
        Bind(config);
    }
}

/// <summary>
/// 扩展配置类，在基础开关上增加 ESC 菜单按钮坐标（PosX, PosY）。
/// </summary>
/// <typeparam name="T">派生类自身类型（用于单例）</typeparam>
public abstract class MenuConfigBase<T> : ConfigBase<T> where T : MenuConfigBase<T>, new()
{
    public ConfigEntry<int> PosX { get; protected set; } = null!;
    public ConfigEntry<int> PosY { get; protected set; } = null!;

    public override void Bind(ConfigFile config)
    {
        base.Bind(config);
        PosX = config.Bind("UI", "PosX", 200, "Button X offset.");
        PosY = config.Bind("UI", "PosY", 100, "Button Y offset.");
    }
}