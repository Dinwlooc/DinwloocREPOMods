using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace MonsterCombatGroup
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("Dinwlooc.Common", BepInDependency.DependencyFlags.HardDependency)]
    public class MonsterCombatGroup : BaseUnityPlugin
    {
        internal static MonsterCombatGroup Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;

        private MonsterCombatService? _service;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            // 初始化配置
            MonsterCombatGroupConfig.Instance.Initialize(Config);

            Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} loaded.");
        }

        private void Start()
        {
            if (_service == null)
            {
                _service = gameObject.AddComponent<MonsterCombatService>();
                Logger.LogInfo("MonsterCombatService created.");
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                Destroy(_service);
                _service = null;
            }
        }
    }

    internal static class PluginInfo
    {
        public const string PLUGIN_GUID = "Dinwlooc.MonsterCombatGroup";
        public const string PLUGIN_NAME = "MonsterCombatGroup";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}