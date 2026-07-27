using System.IO;

namespace SuperEnergy
{
    public class RemoteStaminaConfig
    {
        public int Percent { get; }
        public bool CompensateWhenDisabled { get; }
        public bool EnableCrouchBoost { get; }
        public int SlideBoostPercent { get; } // 新增

        public RemoteStaminaConfig(int percent, bool comp, bool crouch, int slideBoostPercent = 0)
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

        public static RemoteStaminaConfig Read(BinaryReader reader)
        {
            int percent = reader.ReadInt32();
            bool comp = reader.ReadBoolean();
            bool crouch = reader.ReadBoolean();
            int slideBoost = reader.ReadInt32();
            return new RemoteStaminaConfig(percent, comp, crouch, slideBoost);
        }
    }
}