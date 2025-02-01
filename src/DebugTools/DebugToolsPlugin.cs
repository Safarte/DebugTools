using System.Reflection;
using BepInEx;
using JetBrains.Annotations;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI.Appbar;
using DebugTools.UI;
using DebugTools.Utils;
using KSP.Game;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace DebugTools;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class DebugToolsPlugin : BaseSpaceWarpPlugin
{
    // Useful in case some other mod wants to use this mod a dependency
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    /// Singleton instance of the plugin class
    [PublicAPI]
    public static DebugToolsPlugin Instance { get; set; }

    public DebugWindowController DebugWindowController { get; set; }

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        // Load all the other assemblies used by this mod
        LoadAssemblies();

        Settings.Initialize();

        // Create main debug window
        var debugWindow = CreateWindow("DebugWindow");
        // Add a controller for the UI to the window's game object
        DebugWindowController = debugWindow.gameObject.AddComponent<DebugWindowController>();
        DebugWindowController.IsWindowOpen = false;

        CreateDebugWindows();
    }

    private static UIDocument CreateWindow(string name, Transform parent = null)
    {
        // Load the UI from the asset bundle
        var uxml = AssetManager.GetAsset<VisualTreeAsset>(
            $"{ModGuid}/DebugTools_ui/ui/{name}.uxml"
        );

        // Create the window options object
        var windowOptions = new WindowOptions
        {
            WindowId = $"DebugTools_{name}",
            Parent = parent,
            IsHidingEnabled = true,
            DisableGameInputForTextFields = true,
            MoveOptions = new MoveOptions
            {
                IsMovingEnabled = true,
                CheckScreenBounds = true
            }
        };

        // Create the window
        return Window.Create(windowOptions, uxml);
    }

    /// <summary>
    /// Loads all the assemblies for the mod.
    /// </summary>
    private static void LoadAssemblies()
    {
        // Load the Unity project assembly
        var currentFolder = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName;
        var unityAssembly = Assembly.LoadFrom(Path.Combine(currentFolder, "DebugTools.Unity.dll"));
        // Register any custom UI controls from the loaded assembly
        CustomControls.RegisterFromAssembly(unityAssembly);
    }

    private void CreateDebugWindows()
    {
        // Thermal data window
        var thermalDataWindow = CreateWindow("ThermalDataWindow");
        var thermalDataWindowController = thermalDataWindow.gameObject.AddComponent<ThermalDataWindowController>();
        thermalDataWindowController.IsWindowOpen = false;
        DebugWindowController.ThermalToggle.RegisterCallback((ChangeEvent<bool> evt) =>
            thermalDataWindowController.IsWindowOpen = evt.newValue);
    }

    public void Update()
    {
        if (Settings.ToggleKey?.Value != null && Settings.ToggleKey.Value.IsDown())
        {
            DebugWindowController.IsWindowOpen = !DebugWindowController.IsWindowOpen;
        }
    }
}