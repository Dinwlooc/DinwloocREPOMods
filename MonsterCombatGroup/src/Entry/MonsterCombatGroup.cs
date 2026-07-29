using BepInEx;
using BepInEx.Logging;
using Dinwlooc.Common.Core;
using UnityEngine;
using System.Collections.Generic;

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

            // 注册翻译（跳过 Enabled 等通用键）
            RegisterTranslations();

            Logger.LogInfo($"{PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} loaded.");
        }

        private void RegisterTranslations()
        {
            var translations = new Dictionary<string, string>
            {
                ["Cooldown Seconds"] = "选举冷却(秒)",
                ["Leader Health Multiplier"] = "领队生命倍率",
                ["Guard Health Multiplier"] = "护卫生命倍率",
                ["Duration Seconds"] = "基础眩晕免疫(秒)",
                ["Enable Battery Drain"] = "领队受伤减电量",
                ["Enable Guard Stun Recovery"] = "护卫受伤解眩晕",
                ["Command Interval"] = "指挥攻击间隔(秒)",
                ["Command Attack Count"] = "指挥攻击次数",
                ["Global Stun Immunity"] = "指挥状态免疫(秒)",
                ["Leader Extra PerGuard"] = "每护卫额外免疫(秒)",
                ["Duration"] = "奖励持续(秒)"
            };

            TranslationManager.RegisterTranslations(
                PluginInfo.PLUGIN_GUID,
                "zh",
                1,
                translations
            );
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