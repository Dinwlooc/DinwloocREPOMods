namespace MonsterHighlight.Events
{
    /// <summary>
    /// 当怪物高亮状态成功应用时发布。
    /// </summary>
    public readonly struct MonsterHighlightAppliedEvent
    {
        public readonly int EnemyInstanceId;
        public readonly bool IsHighlighted;

        public MonsterHighlightAppliedEvent(int enemyInstanceId, bool isHighlighted)
        {
            EnemyInstanceId = enemyInstanceId;
            IsHighlighted = isHighlighted;
        }
    }
}