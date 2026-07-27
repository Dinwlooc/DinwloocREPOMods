using System;
using System.Reflection;
using BepInEx.Configuration;
using Dinwlooc.Common.IBridge;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class MenuBridge : IMenuBridge
    {
        private static MenuBridge? _instance;
        public static MenuBridge Instance => _instance ??= new MenuBridge();
        private MenuBridge() { }

        private static readonly MethodInfo? _addElementToEscapeMenuMethod;
        private static readonly MethodInfo? _createREPOButtonMethod;
        private static readonly bool _isAvailable;

        static MenuBridge()
        {
            Type? menuAPIType = Type.GetType("MenuLib.MenuAPI, MenuLib");
            if (menuAPIType != null)
            {
                _addElementToEscapeMenuMethod = menuAPIType.GetMethod("AddElementToEscapeMenu",
                    BindingFlags.Public | BindingFlags.Static);
                _createREPOButtonMethod = menuAPIType.GetMethod("CreateREPOButton",
                    new Type[] { typeof(string), typeof(Action), typeof(Transform), typeof(Vector2) });
                if (_addElementToEscapeMenuMethod != null && _createREPOButtonMethod != null)
                {
                    _isAvailable = true;
                    Core.CommonPlugin.Logger.LogInfo("MenuLib detected and cached.");
                }
                else
                {
                    Core.CommonPlugin.Logger.LogWarning("MenuLib detected but required methods not found.");
                }
            }
            else
            {
                Core.CommonPlugin.Logger.LogInfo("MenuLib not detected; UI functions will be no-op.");
            }
        }

        public void AddEscapeMenuButton(
            string text,
            Action onClick,
            ConfigEntry<bool>? enabledConfig = null,
            ConfigEntry<int>? posXConfig = null,
            ConfigEntry<int>? posYConfig = null)
        {
            if (!_isAvailable) return;
            if (enabledConfig != null && !enabledConfig.Value) return;

            float x = posXConfig?.Value ?? 200f;
            float y = posYConfig?.Value ?? 100f;

            // 使用反射调用 AddElementToEscapeMenu，传入一个委托
            _addElementToEscapeMenuMethod!.Invoke(null, new object[] { (Action<object>)(parent =>
            {
                // 创建按钮：参数顺序 (string text, Action onClick, Transform parent, Vector2 size)
                object button = _createREPOButtonMethod!.Invoke(null, new object[] { text, onClick, parent, new Vector2(x, y) });
                // 激活按钮
                PropertyInfo? gameObjectProp = button.GetType().GetProperty("gameObject");
                if (gameObjectProp != null)
                {
                    GameObject go = gameObjectProp.GetValue(button) as GameObject;
                    if (go != null) go.SetActive(true);
                }
            }) });
        }
    }
}