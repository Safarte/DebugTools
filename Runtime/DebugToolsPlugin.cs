using JetBrains.Annotations;
using DebugTools.Runtime.Controllers;
using DebugTools.Runtime.Controllers.FlightTools;
using DebugTools.Runtime.Controllers.VesselTools;
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
        private static DebugWindowController? DebugWindowController { get; set; }

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

            var debugHandle = UITKHelper.LoadUxml("DebugWindow");
            debugHandle.Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logger.LogError("Failed to load DebugWindow.uxml");
                    return;
                }

                // Create main debug window
                var debugWindow = UITKHelper.CreateWindowFromUxml(handle.Result, "DebugWindow");
                // Add a controller for the UI to the window's game object
                DebugWindowController = debugWindow.gameObject.AddComponent<DebugWindowController>();
                DebugWindowController.IsWindowOpen = false;

                CreateDebugWindows();
                DebugWindowController.RegisterToggle("VFXTestSuite", "VFX Test Suite",
                    evt => GameManager.Instance.Game.VFXTestSuiteDialog.IsVisible = evt.newValue);
                DebugWindowController.RegisterToggle("FXDebugTools", "FX Debug Tools",
                    evt => GameManager.Instance.Game.FXDebugTools.IsVisible = evt.newValue);
            };
        }

        private static void CreateDebugWindow<T>(string windowName, string toggleLabel) where T : BaseWindowController
        {
            // Load window UXML
            var handle = UITKHelper.LoadUxml(windowName);
            handle.Completed += handle1 =>
            {
                if (handle1.Status != AsyncOperationStatus.Succeeded)
                {
                    Logger.LogError($"Failed to load {windowName}.uxml");
                    return;
                }

                // Create UITK window
                var window = UITKHelper.CreateWindowFromUxml(handle1.Result, windowName);

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
            // Vessel tools window
            CreateDebugWindow<VesselToolsWindowController>("VesselToolsWindow", "Vessel Tools");
            // Flight tools window
            CreateDebugWindow<FlightToolsWindowController>("FlightToolsWindow", "Flight Tools");
            // Joints tools window
            CreateDebugWindow<JointsToolsWindowController>("JointsToolsWindow", "Joints Tools");
            // Quick actions window
            CreateDebugWindow<QuickSwitchWindowController>("QuickSwitchWindow", "Quick Switch");
            // Terrain debug window
            CreateDebugWindow<TerrainDebugWindowController>("TerrainDebugWindow", "Terrain Debug");
            // Science tools window
            CreateDebugWindow<ScienceToolsWindowController>("ScienceToolsWindow", "Science Tools");
            // Vessel science window
            CreateDebugWindow<VesselScienceWindowController>("VesselScienceWindow", "Vessel Science");
        }
    }
}