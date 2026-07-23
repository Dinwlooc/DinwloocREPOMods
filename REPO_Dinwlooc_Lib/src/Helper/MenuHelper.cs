using System;
using BepInEx.Configuration;
using MenuLib;
using UnityEngine;

namespace Dinwlooc.Common.Helpers;

public static class MenuHelper
{
    public static void AddEscapeMenuButton(
        string text,
        Action onClick,
        ConfigEntry<bool>? enabledConfig = null,
        ConfigEntry<int>? posXConfig = null,
        ConfigEntry<int>? posYConfig = null)
    {
        if (enabledConfig != null && !enabledConfig.Value)
            return;

        float x = posXConfig?.Value ?? 200f;
        float y = posYConfig?.Value ?? 100f;

        MenuAPI.AddElementToEscapeMenu(parent =>
        {
            var button = MenuAPI.CreateREPOButton(text, onClick, parent, new Vector2(x, y));
            button.gameObject.SetActive(true);
        });
    }
}