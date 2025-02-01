using BepInEx.Configuration;
using UnityEngine;

namespace DebugTools.Utils;

public static class Settings
{
    private static DebugToolsPlugin Plugin => DebugToolsPlugin.Instance;

    public static ConfigEntry<KeyboardShortcut> ToggleKey;

    public static void Initialize()
    {
        ToggleKey = Plugin.Config.Bind("Keybinding", "Toggle Debug UI",
            new KeyboardShortcut(KeyCode.F12, KeyCode.LeftAlt), "Toggle the main debug UI");
    }
}