using System;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEngine;

namespace Dinwlooc.Common.Reflection
{
    /// <summary>
    /// 全局反射缓存，缓存 MethodInfo、PropertyInfo、FieldInfo，减少重复反射。
    /// 提供动态获取方法和预先缓存的常用成员。
    /// </summary>
    public static class ReflectionCache
    {
        // ---------- 动态缓存（按需查找） ----------
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MethodInfo>> _methods
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, MethodInfo>>();
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>> _properties
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>>();
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, FieldInfo>> _fields
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, FieldInfo>>();

        // ---------- 高性能 ChangeType 委托缓存 ----------
        // 缓存 (object) => Convert.ChangeType(object, targetType) 的委托，避免 MethodInfo.Invoke
        private static readonly ConcurrentDictionary<Type, Func<object, object>> _changeTypeCache
            = new ConcurrentDictionary<Type, Func<object, object>>();

        // 预先缓存的 Convert.ChangeType 方法委托（用于生成特化闭包）
        private static readonly Func<object, Type, object> _convertChangeTypeDelegate;

        static ReflectionCache()
        {
            MethodInfo changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
            if (changeTypeMethod == null)
            {
                throw new MissingMethodException("Convert.ChangeType(object, Type) method not found.");
            }
            _convertChangeTypeDelegate = (Func<object, Type, object>)Delegate.CreateDelegate(
                typeof(Func<object, Type, object>), changeTypeMethod);
        }

        /// <summary>
        /// 高性能类型转换，优先使用缓存委托。若值已是目标类型则直接返回。
        /// </summary>
        public static object? ChangeType(object? value, Type conversionType)
        {
            if (value == null) return null;
            if (conversionType == null) throw new ArgumentNullException(nameof(conversionType));

            Type valueType = value.GetType();
            if (conversionType.IsAssignableFrom(valueType)) return value;

            Func<object, object> converter = _changeTypeCache.GetOrAdd(conversionType, t =>
            {
                // 闭包捕获目标类型 t，返回 (object input) => Convert.ChangeType(input, t)
                return (object input) => _convertChangeTypeDelegate(input, t);
            });

            return converter(value);
        }

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

        // ---------- 静态懒加载缓存（常用反射成员） ----------
        // StatsManager
        private static readonly Lazy<FieldInfo> _statsManagerSaveFileCurrent =
            new Lazy<FieldInfo>(() => typeof(StatsManager).GetField("saveFileCurrent", BindingFlags.Instance | BindingFlags.NonPublic));
        public static FieldInfo StatsManager_saveFileCurrent => _statsManagerSaveFileCurrent.Value;

        // Enemy
        private static readonly Lazy<FieldInfo> _enemyRigidbody =
            new Lazy<FieldInfo>(() => typeof(Enemy).GetField("Rigidbody", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        public static FieldInfo Enemy_Rigidbody => _enemyRigidbody.Value;

        // ItemHealthPack
        private static readonly Lazy<MethodInfo> _itemHealthPackUsedRPC =
            new Lazy<MethodInfo>(() => typeof(ItemHealthPack).GetMethod("UsedRPC", BindingFlags.Instance | BindingFlags.NonPublic));
        public static MethodInfo ItemHealthPack_UsedRPC => _itemHealthPackUsedRPC.Value;

        private static readonly Lazy<FieldInfo> _itemHealthPackUsed =
            new Lazy<FieldInfo>(() => typeof(ItemHealthPack).GetField("used", BindingFlags.Instance | BindingFlags.NonPublic));
        public static FieldInfo ItemHealthPack_used => _itemHealthPackUsed.Value;

        private static readonly Lazy<FieldInfo> _itemHealthPackItemToggle =
            new Lazy<FieldInfo>(() => typeof(ItemHealthPack).GetField("itemToggle", BindingFlags.Instance | BindingFlags.NonPublic));
        public static FieldInfo ItemHealthPack_itemToggle => _itemHealthPackItemToggle.Value;

        // PlayerController
        private static readonly Lazy<FieldInfo> _playerControllerSprintRechargeAmount =
            new Lazy<FieldInfo>(() => typeof(PlayerController).GetField("sprintRechargeAmount", BindingFlags.Instance | BindingFlags.NonPublic));
        public static FieldInfo PlayerController_sprintRechargeAmount => _playerControllerSprintRechargeAmount.Value;

        private static readonly Lazy<FieldInfo> _playerControllerSprintRechargeTimer =
            new Lazy<FieldInfo>(() => typeof(PlayerController).GetField("sprintRechargeTimer", BindingFlags.Instance | BindingFlags.NonPublic));
        public static FieldInfo PlayerController_sprintRechargeTimer => _playerControllerSprintRechargeTimer.Value;

        private static readonly Lazy<FieldInfo> _playerControllerSprintRechargeTime =
            new Lazy<FieldInfo>(() => typeof(PlayerController).GetField("sprintRechargeTime", BindingFlags.Instance | BindingFlags.NonPublic));
        public static FieldInfo PlayerController_sprintRechargeTime => _playerControllerSprintRechargeTime.Value;

        private static readonly Lazy<FieldInfo> _playerControllerEnergySprintDrain =
            new Lazy<FieldInfo>(() => typeof(PlayerController).GetField("EnergySprintDrain", BindingFlags.Instance | BindingFlags.Public));
        public static FieldInfo PlayerController_EnergySprintDrain => _playerControllerEnergySprintDrain.Value;

        private static readonly Lazy<FieldInfo> _playerControllerSprintSpeedUpgrades =
            new Lazy<FieldInfo>(() => typeof(PlayerController).GetField("SprintSpeedUpgrades", BindingFlags.Instance | BindingFlags.Public));
        public static FieldInfo PlayerController_SprintSpeedUpgrades => _playerControllerSprintSpeedUpgrades.Value;
    }
}