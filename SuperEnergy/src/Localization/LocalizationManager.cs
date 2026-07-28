using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;

namespace SuperEnergy
{
    internal static class LocalizationManager
    {
        private const int TRANSLATION_VERSION = 2;
        private static readonly string ConfigDir = Path.Combine(Paths.ConfigPath, "Translation", "zh");
        private static readonly string FullPath = Path.Combine(ConfigDir, "Dinwlooc.SuperEnergy.txt");

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);

                bool shouldWrite = false;
                int fileVersion = 0;

                if (File.Exists(FullPath))
                {
                    // 读取文件头获取版本号
                    string[] lines = File.ReadAllLines(FullPath, new UTF8Encoding(false));
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("# Version="))
                        {
                            int.TryParse(line.Substring("# Version=".Length), out fileVersion);
                            break;
                        }
                    }
                    if (fileVersion != TRANSLATION_VERSION)
                    {
                        shouldWrite = true;
                        SuperEnergy.Logger.LogInfo($"翻译文件版本不匹配（文件版本 {fileVersion}，当前版本 {TRANSLATION_VERSION}），将覆盖更新。");
                    }
                }
                else
                {
                    shouldWrite = true;
                }

                if (shouldWrite)
                {
                    using (var sw = new StreamWriter(FullPath, false, new UTF8Encoding(false)))
                    {
                        sw.WriteLine($"# Version={TRANSLATION_VERSION}");
                        sw.WriteLine("# Dinwlooc.SuperEnergy 中文翻译");
                        sw.WriteLine("# 格式: 键名=中文显示名");
                        sw.WriteLine();
                        foreach (string line in GetDefaultLines())
                            sw.WriteLine(line);
                    }
                    SuperEnergy.Logger.LogInfo($"已创建/更新翻译文件：{FullPath}");
                }
            }
            catch (Exception ex)
            {
                SuperEnergy.Logger.LogError($"翻译文件操作失败：{ex.Message}");
            }
        }

        private static string[] GetDefaultLines()
        {
            return new[]
            {
                "Item Charging Enabled=启用",
                "Item Charging Source=充电来源",
                "Item Charging Interval=充电间隔(秒)",
                "Item Charging Amount=每次充电量(%)",
                "Player Heal Enabled=启用",
                "Player Heal Source=自愈来源",
                "Player Heal Interval=自愈间隔(秒)",
                "Player Heal Amount=每次恢复量(HP)",
                "Death Head Revive Enabled=启用",
                "Death Head Revive Required Time=复活所需时间(秒)",
                "Stamina Boost Enabled=启用",
                "Stamina Boost Percent=额外恢复百分比",
                "Stamina Boost Compensate When Disabled=原版禁用时补偿",
                "Stamina Boost Enable Crouch Boost=下蹲加成",
                "Slide Boost Enabled=启用",
                "Slide Boost Percent=滑铲额外百分比",
                "Sync Use Host Config=使用房主配置",
            };
        }
    }
}