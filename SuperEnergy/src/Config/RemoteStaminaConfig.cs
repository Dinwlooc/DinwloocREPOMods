using System.IO;

namespace SuperEnergy
{
    public class RemoteStaminaConfig
    {
        public int Percent { get; }
        public bool CompensateWhenDisabled { get; }
        public bool EnableCrouchBoost { get; }

        public RemoteStaminaConfig(int percent, bool comp, bool crouch)
        {
            Percent = percent;
            CompensateWhenDisabled = comp;
            EnableCrouchBoost = crouch;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Percent);
            writer.Write(CompensateWhenDisabled);
            writer.Write(EnableCrouchBoost);
        }

        public static RemoteStaminaConfig Read(BinaryReader reader)
        {
            int percent = reader.ReadInt32();
            bool comp = reader.ReadBoolean();
            bool crouch = reader.ReadBoolean();
            return new RemoteStaminaConfig(percent, comp, crouch);
        }
    }
}