using System.IO;

namespace SuperEnergy
{
    /// <summary>
    /// 同步配置对象，仅包含需要跨客户端同步的体力/滑铲字段。
    /// </summary>
    public class StaminaSyncConfig
    {
        public int Percent { get; }
        public bool CompensateWhenDisabled { get; }
        public bool EnableCrouchBoost { get; }
        public int SlideBoostPercent { get; }

        public StaminaSyncConfig(int percent, bool comp, bool crouch, int slideBoostPercent = 0)
        {
            Percent = percent;
            CompensateWhenDisabled = comp;
            EnableCrouchBoost = crouch;
            SlideBoostPercent = slideBoostPercent;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Percent);
            writer.Write(CompensateWhenDisabled);
            writer.Write(EnableCrouchBoost);
            writer.Write(SlideBoostPercent);
        }

        public static StaminaSyncConfig Read(BinaryReader reader)
        {
            int percent = reader.ReadInt32();
            bool comp = reader.ReadBoolean();
            bool crouch = reader.ReadBoolean();
            int slideBoost = reader.ReadInt32();
            return new StaminaSyncConfig(percent, comp, crouch, slideBoost);
        }
    }
}