using System.Collections.Generic;
using DebugTools.Utils;
using KSP.Game;
using KSP.Messages;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    /// <summary>
    /// Controller for the DebugWindow UI.
    /// </summary>
    public class DebugWindowController : KerbalMonoBehaviour
    {
        // The UIDocument component of the window game object
        private UIDocument _window;

        // The elements of the window that we need to access
        private VisualElement _rootElement;
        private VisualElement _content;

        // Debug tool windows toggles
        public Dictionary<string, Toggle> WindowToggles = new();

        // The backing field for the IsWindowOpen property
        private bool _isWindowOpen;

        /// <summary>
        /// The state of the window. Setting this value will open or close the window.
        /// </summary>
        public bool IsWindowOpen
        {
            get => _isWindowOpen;
            set
            {
                _isWindowOpen = value;
                // Set the display style of the root element to show or hide the window
                _rootElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void Awake()
        {
            Game.Messages.Subscribe<GameStateEnteredMessage>(OnGameStateEntered);
        }

        /// <summary>
        /// Runs when the window is first created, and every time the window is re-enabled.
        /// </summary>
        private void OnEnable()
        {
            // Get the UIDocument component from the game object
            _window = GetComponent<UIDocument>();

            // Get the root element of the window.
            // Since we're cloning the UXML tree from a VisualTreeAsset, the actual root element is a TemplateContainer,
            // so we need to get the first child of the TemplateContainer to get our actual root VisualElement.
            _rootElement = _window.rootVisualElement[0];

            // Center the window by default
            _rootElement.CenterByDefault();

            // Get the close button from the window
            var closeButton = _rootElement.Q<Button>("close-button");
            // Add a click event handler to the close button
            closeButton.clicked += () => IsWindowOpen = false;
            
            // Toggles container
            _content = _rootElement.Q<VisualElement>("content");
            _content.Clear();
        }

        public void Update()
        {
            if (Input.GetKey(Configuration.ToggleModifierKey.Value) && Input.GetKeyDown(Configuration.ToggleKey.Value))
            {
                IsWindowOpen = !IsWindowOpen;
            }
        }

        public void RegisterToggle(string windowName, string toggleLabel, EventCallback<ChangeEvent<bool>> callback)
        {
            WindowToggles[windowName] = new Toggle
            {
                label = toggleLabel
            };
            WindowToggles[windowName].AddToClassList("toggle");
            _content.Add(WindowToggles[windowName]);
            WindowToggles[windowName].RegisterCallback(callback);
        }

        private void OnGameStateEntered(MessageCenterMessage msg)
        {
            if (msg is not GameStateEnteredMessage message) return;
            
            foreach (var toggle in WindowToggles.Values)
                toggle?.SetEnabled((int)message.StateBeingEntered > 2);
        }
    }
}