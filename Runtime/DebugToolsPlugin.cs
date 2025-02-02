using JetBrains.Annotations;
using DebugTools.UI;
using DebugTools.Utils;
using KSP.Game;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using ILogger = ReduxLib.Logging.ILogger;

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

        private static void CreateDebugWindows()
        {
            // Thermal data window
            var thermalHandle = LoadUxml("ThermalDataWindow");
            thermalHandle.Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logger.LogError("Failed to load ThermalDataWindow.uxml");
                    return;
                }

                var thermalDataWindow = CreateWindowFromUxml(handle.Result, "ThermalDataWindow");
                var thermalDataWindowController =
                    thermalDataWindow.gameObject.AddComponent<ThermalDataWindowController>();
                DebugWindowController.ThermalToggle.RegisterCallback((ChangeEvent<bool> evt) =>
                    thermalDataWindowController.IsWindowOpen = evt.newValue);
            };
        }

        public void Update()
        {
            if (Input.GetKey(Configuration.ToggleModifierKey.Value) && Input.GetKeyDown(Configuration.ToggleKey.Value))
            {
                DebugWindowController.IsWindowOpen = !DebugWindowController.IsWindowOpen;
            }
        }
    }
}