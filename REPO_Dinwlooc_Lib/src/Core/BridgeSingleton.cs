using System;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 纯 C# 单例基类，为所有桥接服务提供统一的懒加载实现。
    /// 使用 Lazy&lt;T&gt; 保证线程安全，通过 Activator 调用私有构造函数。
    /// </summary>
    /// <typeparam name="T">派生类自身类型，必须具有私有或受保护的构造函数</typeparam>
    public abstract class BridgeSingleton<T> where T : class
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() =>
            (T)Activator.CreateInstance(typeof(T), true));

        public static T Instance => _instance.Value;
    }
}