using System;
using KSP.Game;
using UitkForKsp2.API;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace DebugTools.Utils
{
    public static class UITKHelper
    {
        public static void LoadUxml(string name, Action<VisualTreeAsset> callback)
        {
            GameManager.Instance.Assets.Load($"Assets/Modules/DebugTools/Assets/UI/{name}.uxml", callback);
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