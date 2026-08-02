using System;
using System.Reflection;
using BepInEx.Configuration;
using Dinwlooc.Common.Core;
using Dinwlooc.Common.IBridge;
using MenuLib;
using UnityEngine;

namespace Dinwlooc.Common.Bridge
{
    public class MenuBridge : BridgeSingleton<MenuBridge>, IMenuBridge
    {
        private const string MenuLibTypeName = "MenuLib.MenuAPI, MenuLib";
        private const string AddElementMethodName = "AddElementToEscapeMenu";
        private const string CreateButtonMethodName = "CreateREPOButton";

        private static readonly MethodInfo AddElementToEscapeMenuMethod;
        private static readonly MethodInfo CreateREPOButtonMethod;
        private static readonly bool IsMenuLibAvailable;

        static MenuBridge()
        {
            Type menuAPIType = Type.GetType(MenuLibTypeName);
            if (menuAPIType != null)
            {
                AddElementToEscapeMenuMethod = menuAPIType.GetMethod(AddElementMethodName,
                    BindingFlags.Public | BindingFlags.Static);
                CreateREPOButtonMethod = menuAPIType.GetMethod(CreateButtonMethodName,
                    new Type[] { typeof(string), typeof(Action), typeof(Transform), typeof(Vector2) });
                if (AddElementToEscapeMenuMethod != null && CreateREPOButtonMethod != null)
                {
                    IsMenuLibAvailable = true;
                    CommonPlugin.Logger.LogInfo("MenuLib detected and cached.");
                }
                else
                {
                    CommonPlugin.Logger.LogWarning("MenuLib detected but required methods not found.");
                }
            }
            else
            {
                CommonPlugin.Logger.LogInfo("MenuLib not detected; UI functions will be no-op.");
            }
        }

        private MenuBridge() { }

        public void AddEscapeMenuButton(
            string text,
            Action onClick,
            ConfigEntry<bool> enabledConfig = null,
            ConfigEntry<int> posXConfig = null,
            ConfigEntry<int> posYConfig = null)
        {
            if (!IsMenuLibAvailable) return;
            if (enabledConfig != null && !enabledConfig.Value) return;

            float x = posXConfig?.Value ?? 200f;
            float y = posYConfig?.Value ?? 100f;

            MenuAPI.BuilderDelegate builderDelegate = new MenuAPI.BuilderDelegate(parent =>
            {
                object button = CreateREPOButtonMethod.Invoke(null, new object[] { text, onClick, parent, new Vector2(x, y) });
                PropertyInfo gameObjectProp = button.GetType().GetProperty("gameObject");
                if (gameObjectProp != null)
                {
                    GameObject go = gameObjectProp.GetValue(button) as GameObject;
                    if (go != null) go.SetActive(true);
                }
            });

            AddElementToEscapeMenuMethod.Invoke(null, new object[] { builderDelegate });
        }
    }
}