using JetBrains.Annotations;
using DebugTools.Runtime.Controllers;
using DebugTools.Runtime.Controllers.FlightTools;
using DebugTools.Utils;
using KSP.Game;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using ILogger = ReduxLib.Logging.ILogger;

// ReSharper disable once CheckNamespace
namespace DebugTools
{
    public class DebugToolsPlugin : KerbalMonoBehaviour
    {
        /// Singleton instance of the plugin class
        [PublicAPI]
        public static DebugToolsPlugin Instance { get; set; }

        public static DebugWindowController DebugWindowController { get; set; }

        internal static ILogger Logger;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void AttachToReduxLib()
        {
            ReduxLib.ReduxLib.OnReduxLibInitialized += PreInitialize;
        }

        private static void PreInitialize()
        {
            Logger = ReduxLib.ReduxLib.GetLogger("Debug Tools");
            Logger.LogInfo("Pre-initialized");
        }

        /// <summary>
        /// Runs when the mod is first initialized.
        /// </summary>
        public static void Initialize()
        {
            Configuration.Initialize(ReduxLib.ReduxLib.ReduxCoreConfig);

            var debugHandle = LoadUxml("DebugWindow");
            debugHandle.Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logger.LogError("Failed to load DebugWindow.uxml");
                    return;
                }

                // Create main debug window
                var debugWindow = CreateWindowFromUxml(handle.Result, "DebugWindow");
                // Add a controller for the UI to the window's game object
                DebugWindowController = debugWindow.gameObject.AddComponent<DebugWindowController>();
                DebugWindowController.IsWindowOpen = false;
            };

            CreateDebugWindows();
        }

        private static AsyncOperationHandle<VisualTreeAsset> LoadUxml(string name)
        {
            return Addressables.LoadAssetAsync<VisualTreeAsset>($"Assets/Modules/DebugTools/Assets/UI/{name}.uxml");
        }

        private static UIDocument CreateWindowFromUxml(VisualTreeAsset uxml, string name)
        {
            // Create the window options object
            var windowOptions = new WindowOptions
            {
                WindowId = $"DebugTools_{name}",
                Parent = null,
                IsHidingEnabled = true,
                DisableGameInputForTextFields = true,
                MoveOptions = new MoveOptions
                {
                    IsMovingEnabled = true,
                    CheckScreenBounds = true
                }
            };

            // Create the window
            Instantiate(uxml);
            return Window.Create(windowOptions, uxml);
        }

        private static void CreateDebugWindow<T>(string windowName, string toggleLabel) where T : BaseWindowController
        {
            // Load window UXML
            var handle = LoadUxml(windowName);
            handle.Completed += handle1 =>
            {
                if (handle1.Status != AsyncOperationStatus.Succeeded)
                {
                    Logger.LogError($"Failed to load {windowName}.uxml");
                    return;
                }

                // Create UITK window
                var window = CreateWindowFromUxml(handle1.Result, windowName);

                // Attach window controller
                var controller = window.gameObject.AddComponent<T>();
                controller.IsWindowOpen = false;

                // Setup window toggling & close button
                DebugWindowController.RegisterToggle(windowName, toggleLabel,
                    evt => controller.IsWindowOpen = evt.newValue);
                controller.CloseButton.clicked += () =>
                {
                    controller.IsWindowOpen = false;
                    DebugWindowController.WindowToggles[windowName].value = false;
                };
            };
        }

        private static void CreateDebugWindows()
        {
            // Thermal data window
            CreateDebugWindow<ThermalDataWindowController>("ThermalDataWindow", "Thermal Data");
            
            // Flight tools window
            CreateDebugWindow<FlightToolsWindowController>("FlightToolsWindow", "Flight Tools");
            
            // Quick actions window
            CreateDebugWindow<QuickSwitchWindowController>("QuickSwitchWindow", "Quick Switch");
            
            // Terrain debug window
            CreateDebugWindow<TerrainDebugWindowController>("TerrainDebugWindow", "Terrain Debug");
            
            // Rendering debug window
            CreateDebugWindow<RenderingDebugWindowController>("RenderingDebugWindow", "Rendering Debug");
        }
    }
}