using System;
using System.Collections.Generic;
using System.IO;

namespace Dinwlooc.Common.Sync
{
    /// <summary>
    /// 轻量级内存流对象池，用于减少序列化/反序列化过程中的内存分配。
    /// 最大池容量 20，超过上限的流将被自然回收。
    /// </summary>
    internal static class ByteBufferPool
    {
        private static readonly Stack<MemoryStream> _pool = new Stack<MemoryStream>();
        private const int MaxPoolSize = 20;
        private static readonly object _lock = new object();

        public static MemoryStream Rent()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                    return _pool.Pop();
            }
            return new MemoryStream();
        }

        public static void Return(MemoryStream ms)
        {
            if (ms == null) return;
            ms.SetLength(0);
            ms.Position = 0;
            lock (_lock)
            {
                if (_pool.Count < MaxPoolSize)
                    _pool.Push(ms);
            }
        }
    }
}