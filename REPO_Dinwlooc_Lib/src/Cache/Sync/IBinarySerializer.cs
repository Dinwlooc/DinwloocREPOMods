using System.IO;

namespace Dinwlooc.Common.Sync
{
    public interface IBinarySerializer<T>
    {
        void Serialize(BinaryWriter writer, T value);
        T Deserialize(BinaryReader reader);
    }
}