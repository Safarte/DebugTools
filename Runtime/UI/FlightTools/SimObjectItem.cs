using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.UI.FlightTools
{
    public class SimObjectItem : VisualElement
    {
        private const string ClassName = "sim-object-item";
        private const string NameClassName = ClassName + "_name";
        private const string PhysicsModeClassName = ClassName + "_physics-mode";
        private const string ParentFrameClassName = ClassName + "_parent-frame";
        private const string ToggleLoadUnloadClassName = ClassName + "_load-unload";

        public readonly Label TextName;
        public readonly Label TextPhysicsMode;
        public readonly Label TextParentReferenceFrame;
        public readonly Button ToggleLoadUnload;
        public readonly Button SetActive;
        public readonly Button Destroy;

        public SimObjectItem()
        {
            AddToClassList(ClassName);

            TextName = new Label();
            TextName.AddToClassList(NameClassName);
            hierarchy.Add(TextName);

            TextPhysicsMode = new Label();
            TextPhysicsMode.AddToClassList(PhysicsModeClassName);
            hierarchy.Add(TextPhysicsMode);

            TextParentReferenceFrame = new Label();
            TextParentReferenceFrame.AddToClassList(ParentFrameClassName);
            hierarchy.Add(TextParentReferenceFrame);

            ToggleLoadUnload = new Button { text = "Unload" };
            ToggleLoadUnload.AddToClassList(ToggleLoadUnloadClassName);
            hierarchy.Add(ToggleLoadUnload);

            SetActive = new Button { text = "Set Active" };
            hierarchy.Add(SetActive);

            Destroy = new Button { text = "Destroy" };
            hierarchy.Add(Destroy);
        }

        public new class UxmlFactory : UxmlFactory<SimObjectItem, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _isLoaded = new() { name = "IsLoaded", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is SimObjectItem item)
                {
                    item.TextName.text = "Waow";
                    item.TextPhysicsMode.text = "RigidBody";
                    item.TextParentReferenceFrame.text = "loremipsum - Celestial";
                    item.ToggleLoadUnload.text = _isLoaded.GetValueFromBag(bag, cc) ? "Unload" : "Load";
                }
            }
        }
    }
}