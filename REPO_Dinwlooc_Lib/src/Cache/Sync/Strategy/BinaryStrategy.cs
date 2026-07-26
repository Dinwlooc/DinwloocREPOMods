using System;
using System.IO;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 基于二进制流的序列化策略（高性能，需用户提供读写委托）。
    /// </summary>
    public class BinaryStrategy<T> : ISerializationStrategy<T>
    {
        private readonly Action<BinaryWriter, T> _serialize;
        private readonly Func<BinaryReader, T> _deserialize;

        public BinaryStrategy(Action<BinaryWriter, T> serialize, Func<BinaryReader, T> deserialize)
        {
            _serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            _deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
        }

        public object SerializeToObject(T value)
        {
            throw new NotSupportedException("二进制策略不支持 Hashtable 序列化。");
        }

        public T DeserializeFromObject(object data)
        {
            throw new NotSupportedException("二进制策略不支持 Hashtable 反序列化。");
        }

        public byte[] SerializeToBinary(T value)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    _serialize(writer, value);
                    return ms.ToArray();
                }
            }
        }

        public T DeserializeFromBinary(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    return _deserialize(reader);
                }
            }
        }
    }
}