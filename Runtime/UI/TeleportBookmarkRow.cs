using System;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.UI
{
    public class TeleportBookmarkRow : VisualElement
    {
        private const string ClassName = "teleport-bookmark-row";
        private const string SelectedClassName = ClassName + "__selected";
        private const string NameClassName = ClassName + "__name";
        private const string BodyNameClassName = ClassName + "__body-name";
        private const string TypeClassName = ClassName + "__type";
        private const string TeleportButtonClassName = ClassName + "__teleport-button";
        private const string DeleteButtonClassName = ClassName + "__delete-button";

        public readonly Label Name;
        public readonly Label BodyName;
        public readonly Label Type;

        private readonly Button _teleportButton;
        private readonly Button _deleteButton;

        public Action<string> OnTeleport;
        public Action<string> OnDelete;

        public TeleportBookmarkRow()
        {
            AddToClassList(ClassName);

            Name = new Label();
            Name.AddToClassList(NameClassName);
            hierarchy.Add(Name);

            BodyName = new Label();
            BodyName.AddToClassList(BodyNameClassName);
            hierarchy.Add(BodyName);

            Type = new Label();
            Type.AddToClassList(TypeClassName);
            hierarchy.Add(Type);

            _teleportButton = new Button();
            _teleportButton.text = "TP";
            _teleportButton.AddToClassList(TeleportButtonClassName);
            _teleportButton.clicked += () => OnTeleport?.Invoke(Name.text);
            hierarchy.Add(_teleportButton);

            _deleteButton = new Button();
            _deleteButton.text = "Del";
            _deleteButton.AddToClassList(DeleteButtonClassName);
            _deleteButton.clicked += () => OnDelete?.Invoke(Name.text);
            hierarchy.Add(_deleteButton);
        }

        public void SetSelected(bool value)
        {
            EnableInClassList(SelectedClassName, value);
        }

        public new class UxmlFactory : UxmlFactory<TeleportBookmarkRow, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is TeleportBookmarkRow row)
                {
                    row.Name.text = "Rings";
                    row.BodyName.text = "Dres";
                    row.Type.text = "Orbit";
                }
            }
        }
    }
}