using System.Collections.Generic;
using UnityEngine;

namespace Dinwlooc.Common.src.Bridge.IBridge
{
    /// <summary>
    /// 敌人系统桥接接口，用于解耦怪物操作
    /// </summary>
    public interface IEnemyBridge
    {
        /// <summary>获取当前场景中所有敌人（包括无效或未激活的）</summary>
        IReadOnlyList<EnemyParent> GetAllEnemies();

        /// <summary>判断敌人是否有效（存在、已生成、存活、激活）</summary>
        bool IsEnemyValid(EnemyParent enemy);

        /// <summary>获取敌人的世界坐标（通常为中心点）</summary>
        Vector3 GetEnemyPosition(EnemyParent enemy);

        /// <summary>获取敌人的 InstanceID</summary>
        int GetEnemyInstanceId(EnemyParent enemy);

        /// <summary>应用自发光高亮效果</summary>
        void ApplyHighlight(EnemyParent enemy, bool active, Color color);

        /// <summary>获取敌人头顶指示器相对于中心点的垂直偏移量</summary>
        float GetIndicatorHeightOffset(EnemyParent enemy);
    }
}