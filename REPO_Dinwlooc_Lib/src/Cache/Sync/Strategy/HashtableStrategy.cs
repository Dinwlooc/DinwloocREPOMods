using System;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 基于 Hashtable 的序列化策略（通用，支持任意类型）。
    /// 直接传递原始对象，无额外序列化开销。
    /// </summary>
    public class HashtableStrategy<T> : ISerializationStrategy<T>
    {
        public object SerializeToObject(T value)
        {
            return value!;
        }

        public T DeserializeFromObject(object data)
        {
            if (data is T typed)
            {
                return typed;
            }
            throw new InvalidCastException($"无法将 {data?.GetType()} 转换为 {typeof(T)}");
        }

        public byte[] SerializeToBinary(T value)
        {
            throw new NotSupportedException("Hashtable 策略不支持二进制序列化。");
        }

        public T DeserializeFromBinary(byte[] data)
        {
            throw new NotSupportedException("Hashtable 策略不支持二进制反序列化。");
        }
    }
}