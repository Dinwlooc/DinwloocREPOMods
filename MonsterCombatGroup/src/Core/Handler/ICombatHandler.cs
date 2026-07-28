namespace MonsterCombatGroup
{
    public interface ICombatHandler
    {
        /// <summary>
        /// 周期性处理逻辑（仅在房主端调用）
        /// </summary>
        /// <param name="deltaTime">间隔时间（秒）</param>
        void Process(float deltaTime);
    }
}