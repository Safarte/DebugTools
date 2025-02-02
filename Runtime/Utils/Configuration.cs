using ReduxLib.Configuration;
using UnityEngine;

namespace DebugTools.Utils
{
    public static class Configuration
    {
        public static ConfigValue<KeyCode> ToggleModifierKey;
        public static ConfigValue<KeyCode> ToggleKey;

        public static void Initialize(IConfigFile config)
        {
            ToggleModifierKey = new ConfigValue<KeyCode>(config.Bind("Keybinding", "Debug UI Modifier Key",
                KeyCode.LeftAlt, "Modifier key to toggle the main debug UI"));

            ToggleKey = new ConfigValue<KeyCode>(config.Bind("Keybinding", "Debug UI Key",
                KeyCode.F12, "Key to toggle the main debug UI"));
        }
    }
}