using System;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.UI
{
    [UxmlElement]
    public partial class MissionRow : VisualElement
    {
        private const string ClassName = "mission-row";
        private const string SelectedClassName = ClassName + "__selected";
        private const string HeaderClassName = ClassName + "__header";
        private const string IdClassName = ClassName + "__id";
        private const string NameClassName = ClassName + "__name";
        private const string TypeClassName = ClassName + "__type";
        private const string OwnerClassName = ClassName + "__owner";
        private const string StateClassName = ClassName + "__state";
        private const string StageInfoClassName = ClassName + "__stage-info";
        private const string SelectButtonClassName = ClassName + "__select-button";

        private const string StateActiveClass = ClassName + "__state--active";
        private const string StateInactiveClass = ClassName + "__state--inactive";
        private const string StateCompleteClass = ClassName + "__state--complete";
        private const string StateFailedClass = ClassName + "__state--failed";

        public readonly Label Id;
        public readonly Label Name;
        public readonly Label Type;
        public readonly Label Owner;
        public readonly Label State;
        public readonly Label StageInfo;

        private readonly Button _selectButton;

        public Action OnSelect;

        [UxmlAttribute]
        public bool IsHeader
        {
            get => _isHeaderValue;
            set
            {
                _isHeaderValue = value;
                if (value)
                {
                    AddToClassList(HeaderClassName);
                    Id.text = "<b>ID</b>";
                    Name.text = "<b>Name</b>";
                    Type.text = "<b>Type</b>";
                    Owner.text = "<b>Owner</b>";
                    State.text = "<b>State</b>";
                    StageInfo.text = "<b>Stage</b>";
                    _selectButton.visible = false;
                }
                else
                {
                    RemoveFromClassList(HeaderClassName);
                    Id.text = "mission_id";
                    Name.text = "Mission Name";
                    Type.text = "Primary";
                    Owner.text = "Agency";
                    State.text = "Active";
                    StageInfo.text = "1/3";
                    _selectButton.visible = true;
                }
            }
        }

        private bool _isHeaderValue;

        public MissionRow()
        {
            AddToClassList(ClassName);

            Id = new Label();
            Id.AddToClassList(IdClassName);
            hierarchy.Add(Id);

            Name = new Label();
            Name.AddToClassList(NameClassName);
            hierarchy.Add(Name);

            Type = new Label();
            Type.AddToClassList(TypeClassName);
            hierarchy.Add(Type);

            Owner = new Label();
            Owner.AddToClassList(OwnerClassName);
            hierarchy.Add(Owner);

            State = new Label();
            State.AddToClassList(StateClassName);
            hierarchy.Add(State);

            StageInfo = new Label();
            StageInfo.AddToClassList(StageInfoClassName);
            hierarchy.Add(StageInfo);

            _selectButton = new Button { text = "Select" };
            _selectButton.AddToClassList(SelectButtonClassName);
            _selectButton.clicked += () => OnSelect?.Invoke();
            hierarchy.Add(_selectButton);

            IsHeader = true;
        }

        public void SetStateClass(string stateText)
        {
            RemoveFromClassList(StateActiveClass);
            RemoveFromClassList(StateInactiveClass);
            RemoveFromClassList(StateCompleteClass);
            RemoveFromClassList(StateFailedClass);

            switch (stateText)
            {
                case "Active":
                    AddToClassList(StateActiveClass);
                    break;
                case "Complete":
                    AddToClassList(StateCompleteClass);
                    break;
                case "Failed":
                    AddToClassList(StateFailedClass);
                    break;
                default:
                    AddToClassList(StateInactiveClass);
                    break;
            }
        }

        public void SetSelected(bool value)
        {
            EnableInClassList(SelectedClassName, value);
        }
    }
}
