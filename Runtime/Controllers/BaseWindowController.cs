using KSP.Game;
using UitkForKsp2.API;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class BaseWindowController : KerbalMonoBehaviour
    {
        // The UIDocument component of the window game object
        protected UIDocument Window;

        // Root VisualElement for the window
        protected VisualElement RootElement;
    
        // The backing field for the IsWindowOpen property
        private bool _isWindowOpen = false;
    
        // Close button
        public Button CloseButton;

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
                RootElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// Runs when the window is first created, and every time the window is re-enabled.
        /// </summary>
        protected void Enable()
        {
            // Get the UIDocument component from the game object
            Window = GetComponent<UIDocument>();

            // Get the root element of the window.
            // Since we're cloning the UXML tree from a VisualTreeAsset, the actual root element is a TemplateContainer,
            // so we need to get the first child of the TemplateContainer to get our actual root VisualElement.
            RootElement = Window.rootVisualElement[0];

            // Center the window by default
            RootElement.CenterByDefault();

            // Get the close button from the window
            CloseButton = RootElement.Q<Button>("close-button");
        }
    }
}