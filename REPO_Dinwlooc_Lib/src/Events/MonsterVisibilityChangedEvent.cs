// 在 Dinwlooc.Common.Events 命名空间
namespace Dinwlooc.Common.Events
{
    public readonly struct MonsterVisibilityChangedEvent
    {
        public readonly int EnemyInstanceId;
        public readonly bool IsVisible;

        public MonsterVisibilityChangedEvent(int enemyInstanceId, bool isVisible)
        {
            EnemyInstanceId = enemyInstanceId;
            IsVisible = isVisible;
        }
    }
}