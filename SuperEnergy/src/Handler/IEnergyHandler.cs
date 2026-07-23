namespace SuperEnergy
{
    public interface IEnergyHandler
    {
        void Process(bool isHost, float deltaTime);
    }
}