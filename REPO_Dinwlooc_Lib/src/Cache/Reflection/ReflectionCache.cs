using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Dinwlooc.Common.Reflection
{
    /// <summary>
    /// 全局反射缓存，缓存 MethodInfo、PropertyInfo、FieldInfo，减少重复反射。
    /// 供所有模块复用，是继普通缓存和同步缓存之后的第三大缓存库。
    /// </summary>
    public static class ReflectionCache
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MethodInfo>> _methods
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, MethodInfo>>();
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>> _properties
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>>();
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, FieldInfo>> _fields
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, FieldInfo>>();

        private static ConcurrentDictionary<string, MethodInfo> GetMethodDict(Type type)
        {
            return _methods.GetOrAdd(type, _ => new ConcurrentDictionary<string, MethodInfo>());
        }

        private static ConcurrentDictionary<string, PropertyInfo> GetPropertyDict(Type type)
        {
            return _properties.GetOrAdd(type, _ => new ConcurrentDictionary<string, PropertyInfo>());
        }

        private static ConcurrentDictionary<string, FieldInfo> GetFieldDict(Type type)
        {
            return _fields.GetOrAdd(type, _ => new ConcurrentDictionary<string, FieldInfo>());
        }

        public static MethodInfo GetMethod(Type type, string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
        {
            var dict = GetMethodDict(type);
            if (dict.TryGetValue(name, out MethodInfo? method))
            {
                return method;
            }

            method = type.GetMethod(name, bindingFlags);
            if (method != null)
            {
                dict[name] = method;
            }
            return method!;
        }

        public static PropertyInfo GetProperty(Type type, string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
        {
            var dict = GetPropertyDict(type);
            if (dict.TryGetValue(name, out PropertyInfo? prop))
            {
                return prop;
            }

            prop = type.GetProperty(name, bindingFlags);
            if (prop != null)
            {
                dict[name] = prop;
            }
            return prop!;
        }

        public static FieldInfo GetField(Type type, string name, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
        {
            var dict = GetFieldDict(type);
            if (dict.TryGetValue(name, out FieldInfo? field))
            {
                return field;
            }

            field = type.GetField(name, bindingFlags);
            if (field != null)
            {
                dict[name] = field;
            }
            return field!;
        }
    }
}