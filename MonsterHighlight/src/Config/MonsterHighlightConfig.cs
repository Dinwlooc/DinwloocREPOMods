using System;
using BepInEx.Configuration;
using Dinwlooc.Common.Helpers;
using UnityEngine;

namespace MonsterHighlight
{
    public enum EHighlightPreset
    {
        Cyan, Blue, Red, Green, Yellow, Orange, Pink, Purple, White
    }

    public class MonsterHighlightConfig : ConfigBase<MonsterHighlightConfig>
    {
        public ConfigEntry<EHighlightPreset> HighlightPreset { get; private set; } = null!;
        public ConfigEntry<bool> EnableEmission { get; private set; } = null!;
        public ConfigEntry<bool> EnableIndicator { get; private set; } = null!;
        public ConfigEntry<int> CheckIntervalMs { get; private set; } = null!;
        public ConfigEntry<int> IndicatorUpdateStep { get; private set; } = null!;
        public ConfigEntry<float> IndicatorSize { get; private set; } = null!;
        public ConfigEntry<int> MinDistance { get; private set; } = null!;
        public ConfigEntry<int> MaxDistance { get; private set; } = null!;
        public ConfigEntry<float> MinSizeRatio { get; private set; } = null!;
        public ConfigEntry<float> IndicatorAlpha { get; private set; } = null!;

        public override void Bind(ConfigFile config)
        {
            base.Bind(config); // 绑定 Enabled

            HighlightPreset = config.Bind("Visual", "HighlightPreset", EHighlightPreset.Cyan, "高亮颜色");
            EnableEmission = config.Bind("Visual", "EnableEmission", true, "启用自发光高亮");
            EnableIndicator = config.Bind("Visual", "EnableIndicator", true, "启用屏幕指示器");
            CheckIntervalMs = config.Bind("Performance", "CheckIntervalMs", 1000,
                new ConfigDescription("视野检测间隔（毫秒）", new AcceptableValueRange<int>(50, 5000)));
            IndicatorUpdateStep = config.Bind("Performance", "IndicatorUpdateStep", 5,
                new ConfigDescription("指示器更新步长（帧数）", new AcceptableValueRange<int>(1, 60)));
            IndicatorSize = config.Bind("Visual", "IndicatorSize", 2.5f,
                new ConfigDescription("指示器基础尺寸缩放", new AcceptableValueRange<float>(0.01f, 10f)));
            MinDistance = config.Bind("Visual", "MinDistance", 0,
                new ConfigDescription("指示器开始缩小的距离（米）", new AcceptableValueRange<int>(0, 200)));
            MaxDistance = config.Bind("Visual", "MaxDistance", 50,
                new ConfigDescription("指示器达到最小尺寸的距离（米）", new AcceptableValueRange<int>(1, 200)));
            MinSizeRatio = config.Bind("Visual", "MinSizeRatio", 0.02f,
                new ConfigDescription("最小尺寸比例", new AcceptableValueRange<float>(0.01f, 1f)));
            IndicatorAlpha = config.Bind("Visual", "IndicatorAlpha", 0.3f,
                new ConfigDescription("指示器透明度", new AcceptableValueRange<float>(0f, 1f)));
        }

        public static Color GetHighlightColor(EHighlightPreset preset) => preset switch
        {
            EHighlightPreset.Cyan => Color.cyan,
            EHighlightPreset.Blue => Color.blue,
            EHighlightPreset.Red => Color.red,
            EHighlightPreset.Green => Color.green,
            EHighlightPreset.Yellow => Color.yellow,
            EHighlightPreset.Orange => new Color(1f, 0.5f, 0f),
            EHighlightPreset.Pink => new Color(1f, 0.41f, 0.71f),
            EHighlightPreset.Purple => new Color(0.5f, 0f, 0.5f),
            EHighlightPreset.White => Color.white,
            _ => Color.cyan,
        };

        public int GetCheckStepFrames()
        {
            int ms = CheckIntervalMs.Value;
            int frames = Mathf.CeilToInt(ms / 16.6667f);
            return Mathf.Clamp(frames, 1, 300);
        }

        public float GetIndicatorUpdateInterval() => IndicatorUpdateStep.Value / 60f;
    }
}