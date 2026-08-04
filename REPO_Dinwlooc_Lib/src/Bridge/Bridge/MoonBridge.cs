// 文件：Dinwlooc.Common.Bridge/MoonBridge.cs
using System;
using System.Collections.Generic;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    /// <summary>
    /// 月相桥接实现，同时提供数据操作与 UI 控制。
    /// 不维护任何注入状态，所有注入/移除操作均为原子调用。
    /// </summary>
    public sealed class MoonBridge : BridgeSingleton<MoonBridge>, IMoonBridge, IMoonUIBridge
    {
        private const string LogPrefix = "[MoonBridge]";

        private MoonBridge() { }

        // ==================== 私有辅助方法 ====================

        private RunManager GetRunManager()
        {
            RunManager instance = RunManager.instance;
            if (instance == null)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + " RunManager.instance is null.");
                return null;
            }
            return instance;
        }

        private Moon GetCurrentMoon()
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return null;
            }

            int level = runManager.moonLevel;
            if (level <= 0)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + string.Format(" Invalid moon level: {0}", level));
                return null;
            }

            List<Moon> moons = runManager.moons;
            if (moons == null || moons.Count < level)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + string.Format(" Moon index {0} out of range (count: {1})", level, moons?.Count ?? 0));
                return null;
            }

            return moons[level - 1];
        }

        private Moon GetMoonByIndex(int index)
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return null;
            }

            if (index <= 0)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + string.Format(" Invalid index: {0} (must be >= 1)", index));
                return null;
            }

            List<Moon> moons = runManager.moons;
            if (moons == null || moons.Count < index)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + string.Format(" Moon index {0} out of range (count: {1})", index, moons?.Count ?? 0));
                return null;
            }

            return moons[index - 1];
        }

        private MoonUI GetMoonUI()
        {
            MoonUI instance = MoonUI.instance;
            if (instance == null)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + " MoonUI.instance is null.");
                return null;
            }
            return instance;
        }

        // ==================== IMoonBridge 实现 ====================

        public int GetCurrentMoonLevel()
        {
            RunManager runManager = GetRunManager();
            return runManager != null ? runManager.moonLevel : 0;
        }

        public string GetCurrentMoonName()
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return string.Empty;
            }
            return runManager.MoonGetName(runManager.moonLevel);
        }

        public Texture GetCurrentMoonIcon()
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return null;
            }
            return runManager.MoonGetIcon(runManager.moonLevel);
        }

        public IReadOnlyList<Moon.MoonAttribute> GetCurrentMoonAttributes()
        {
            return GetMoonAttributes(GetCurrentMoonLevel());
        }

        public string GetMoonName(int index)
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return string.Empty;
            }
            return runManager.MoonGetName(index);
        }

        public Texture GetMoonIcon(int index)
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return null;
            }
            return runManager.MoonGetIcon(index);
        }

        public IReadOnlyList<Moon.MoonAttribute> GetMoonAttributes(int index)
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return Array.Empty<Moon.MoonAttribute>();
            }

            List<Moon.MoonAttribute> attributes = runManager.MoonGetAttributes(index);
            if (attributes == null)
            {
                return Array.Empty<Moon.MoonAttribute>();
            }

            return attributes;
        }

        public Moon.MoonAttribute InjectAttributeToCurrentMoon(string text)
        {
            int level = GetCurrentMoonLevel();
            if (level <= 0)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + " Inject failed: current moon level is invalid.");
                return null;
            }
            return InjectAttributeToMoon(level, text);
        }

        public Moon.MoonAttribute InjectAttributeToMoon(int index, string text)
        {
            Moon moon = GetMoonByIndex(index);
            if (moon == null)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + string.Format(" Inject failed: moon index {0} not found.", index));
                return null;
            }

            if (string.IsNullOrEmpty(text))
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + " Inject failed: text is null or empty.");
                return null;
            }

            if (moon.moonAttributes == null)
            {
                moon.moonAttributes = new List<Moon.MoonAttribute>();
            }

            Moon.MoonAttribute newAttribute = new Moon.MoonAttribute
            {
                text = text,
                LocalizedText = null
            };

            moon.moonAttributes.Add(newAttribute);

            CommonPlugin.Logger.LogInfo(LogPrefix + string.Format(" Injected attribute \"{0}\" to moon \"{1}\" (index: {2})", text, moon.moonName, index));

            return newAttribute;
        }

        public bool RemoveAttributeFromCurrentMoon(Moon.MoonAttribute attribute)
        {
            int level = GetCurrentMoonLevel();
            if (level <= 0)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + " Remove failed: current moon level is invalid.");
                return false;
            }
            return RemoveAttributeFromMoon(level, attribute);
        }

        public bool RemoveAttributeFromMoon(int index, Moon.MoonAttribute attribute)
        {
            if (attribute == null)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + " Remove failed: attribute reference is null.");
                return false;
            }

            Moon moon = GetMoonByIndex(index);
            if (moon == null)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + string.Format(" Remove failed: moon index {0} not found.", index));
                return false;
            }

            if (moon.moonAttributes == null)
            {
                CommonPlugin.Logger.LogDebug(LogPrefix + string.Format(" Remove failed: moon {0} has no attributes list.", index));
                return false;
            }

            bool removed = moon.moonAttributes.Remove(attribute);
            if (removed)
            {
                CommonPlugin.Logger.LogInfo(LogPrefix + string.Format(" Removed attribute \"{0}\" from moon \"{1}\" (index: {2})", attribute.text, moon.moonName, index));
            }
            else
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + string.Format(" Remove failed: attribute \"{0}\" not found in moon {1}'s list.", attribute.text, index));
            }

            return removed;
        }

        public int CalculateMoonLevel(int levelsCompleted)
        {
            return (levelsCompleted + 1) / 5;
        }

        public bool HasMoonLevelChanged()
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return false;
            }
            return runManager.moonLevelChanged;
        }

        public bool CheckAndResetMoonLevelChanged()
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return false;
            }

            bool changed = runManager.moonLevelChanged;
            if (changed)
            {
                runManager.moonLevelChanged = false;
                CommonPlugin.Logger.LogDebug(LogPrefix + " Moon level changed flag consumed and reset.");
            }
            return changed;
        }

        public void ForceUpdateMoonLevel()
        {
            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                CommonPlugin.Logger.LogWarning(LogPrefix + " ForceUpdateMoonLevel failed: RunManager is null.");
                return;
            }

            runManager.UpdateMoonLevel();
            CommonPlugin.Logger.LogInfo(LogPrefix + string.Format(" Forced moon level update to: {0}", runManager.moonLevel));
        }

        // ==================== IMoonUIBridge 实现 ====================

        public bool IsMoonUIActive()
        {
            MoonUI ui = GetMoonUI();
            if (ui == null)
            {
                return false;
            }

            GameObject activeObject = ui.objectActive;
            return activeObject != null && activeObject.activeSelf;
        }

        public void ForceShowMoonUI()
        {
            MoonUI ui = GetMoonUI();
            if (ui == null)
            {
                return;
            }

            // 如果 UI 处于非空闲状态，先重置到 None 以允许重新触发
            if (ui.state != MoonUI.State.None && ui.state != MoonUI.State.Hide)
            {
                ui.SetState(MoonUI.State.None);
            }

            RunManager runManager = GetRunManager();
            if (runManager != null)
            {
                runManager.moonLevelChanged = true;

                if (ui.state == MoonUI.State.None)
                {
                    ui.Check();
                }
            }

            CommonPlugin.Logger.LogInfo(LogPrefix + " ForceShowMoonUI executed.");
        }

        public void RefreshMoonUI()
        {
            MoonUI ui = GetMoonUI();
            if (ui == null)
            {
                return;
            }

            if (!IsMoonUIActive())
            {
                CommonPlugin.Logger.LogDebug(LogPrefix + " RefreshMoonUI skipped: UI is not active.");
                return;
            }

            RunManager runManager = GetRunManager();
            if (runManager == null)
            {
                return;
            }

            int level = runManager.moonLevel;
            ui.textTitle.text = runManager.MoonGetName(level);
            ui.attributes = runManager.MoonGetAttributes(level);
            ui.attributesIndex = 0;

            if (ui.state != MoonUI.State.Hide && ui.state != MoonUI.State.None)
            {
                ui.SetState(MoonUI.State.Title);
                CommonPlugin.Logger.LogInfo(LogPrefix + " RefreshMoonUI completed, title animation restarted.");
            }
            else
            {
                ForceShowMoonUI();
            }
        }

        public void ForceHideMoonUI()
        {
            MoonUI ui = GetMoonUI();
            if (ui == null)
            {
                return;
            }

            if (!IsMoonUIActive())
            {
                CommonPlugin.Logger.LogDebug(LogPrefix + " ForceHideMoonUI skipped: UI already inactive.");
                return;
            }

            ui.SetState(MoonUI.State.Hide);
            ui.SetState(MoonUI.State.None);

            CommonPlugin.Logger.LogInfo(LogPrefix + " ForceHideMoonUI executed.");
        }

        public void ResetMoonUIState()
        {
            MoonUI ui = GetMoonUI();
            if (ui == null)
            {
                return;
            }

            ui.SetState(MoonUI.State.None);
            CommonPlugin.Logger.LogDebug(LogPrefix + " ResetMoonUIState completed.");
        }

        public void CheckAndShowIfChanged()
        {
            MoonUI ui = GetMoonUI();
            if (ui == null)
            {
                return;
            }

            ui.Check();
        }
    }
}