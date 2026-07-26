using System.IO;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 序列化策略接口，用于将值序列化为可传输的数据，并在接收端还原。
    /// 支持两种模式：Hashtable（通用）和 Binary（高性能自定义）。
    /// </summary>
    public interface ISerializationStrategy<T>
    {
        /// <summary>
        /// 将值序列化为 object（用于 Hashtable 传输）。
        /// </summary>
        object SerializeToObject(T value);

        /// <summary>
        /// 从 object 反序列化为值（用于 Hashtable 接收）。
        /// </summary>
        T DeserializeFromObject(object data);

        /// <summary>
        /// 将值序列化为 byte[]（用于二进制传输）。
        /// </summary>
        byte[] SerializeToBinary(T value);

        /// <summary>
        /// 从 byte[] 反序列化为值（用于二进制接收）。
        /// </summary>
        T DeserializeFromBinary(byte[] data);
    }
}