

using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using UnityEngine.ResourceManagement.AsyncOperations;

// ReSharper disable once CheckNamespace
namespace DebugTools.Utils
{
    public static class UITKHelper
    {
        public static AsyncOperationHandle<VisualTreeAsset> LoadUxml(string name)
        {
            return Addressables.LoadAssetAsync<VisualTreeAsset>($"Assets/Modules/DebugTools/Assets/UI/{name}.uxml");
        }

        public static UIDocument CreateWindowFromUxml(VisualTreeAsset uxml, string name)
        {
            // Create the window options object
            var windowOptions = new WindowOptions
            {
                WindowId = $"DebugTools_{name}",
                IsHidingEnabled = true,
                DisableGameInputForTextFields = true,
                MoveOptions = new MoveOptions
                {
                    IsMovingEnabled = true,
                    CheckScreenBounds = true
                }
            };

            // Create the window
            Object.Instantiate(uxml);
            return Window.Create(windowOptions, uxml);
        }
    }
}