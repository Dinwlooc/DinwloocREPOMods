// 文件：Dinwlooc.Common.IBridge/IMoonBridge.cs
using System.Collections.Generic;
using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 月相数据原子操作桥接接口。
    /// 所有方法均为无状态调用，不追踪任何注入历史。
    /// </summary>
    public interface IMoonBridge
    {
        // ---- 查询 ----
        int GetCurrentMoonLevel();
        string GetCurrentMoonName();
        Texture GetCurrentMoonIcon();
        IReadOnlyList<Moon.MoonAttribute> GetCurrentMoonAttributes();

        string GetMoonName(int index);
        Texture GetMoonIcon(int index);
        IReadOnlyList<Moon.MoonAttribute> GetMoonAttributes(int index);

        // ---- 注入（原子写入，返回引用供调用方自行管理） ----
        Moon.MoonAttribute InjectAttributeToCurrentMoon(string text);
        Moon.MoonAttribute InjectAttributeToMoon(int index, string text);

        // ---- 移除（原子删除，需调用方传入之前获得的引用） ----
        bool RemoveAttributeFromCurrentMoon(Moon.MoonAttribute attribute);
        bool RemoveAttributeFromMoon(int index, Moon.MoonAttribute attribute);

        // ---- 等级计算与状态读取 ----
        int CalculateMoonLevel(int levelsCompleted);
        bool HasMoonLevelChanged();
        bool CheckAndResetMoonLevelChanged();
        void ForceUpdateMoonLevel();
    }
}