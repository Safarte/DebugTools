using UnityEngine.UIElements;

namespace DebugTools.Runtime.UI.VesselTools
{
    public class ManeuverNodeRow : VisualElement
    {
        private const string ClassName = "maneuver-node";
        private const string PartNameClassName = ClassName + "__name";
        private const string SubItemClassName = ClassName + "__item";

        public readonly Label NodeName;
        public readonly Label UniversalTime;
        public readonly Label DeltaV;
        public readonly Label GUID;

        public ManeuverNodeRow()
        {
            AddToClassList(ClassName);

            NodeName = new Label();
            NodeName.AddToClassList(PartNameClassName);
            hierarchy.Add(NodeName);

            UniversalTime = new Label();
            UniversalTime.AddToClassList(SubItemClassName);
            hierarchy.Add(UniversalTime);

            DeltaV = new Label();
            DeltaV.AddToClassList(SubItemClassName);
            hierarchy.Add(DeltaV);

            GUID = new Label();
            GUID.AddToClassList(SubItemClassName);
            hierarchy.Add(GUID);
        }

        public ManeuverNodeRow(bool isHeader) : this()
        {
            if (!isHeader) return;
            
            NodeName.text = "Name";
            UniversalTime.text = "UT";
            DeltaV.text = "Δv";
            GUID.text = "GUID";
        }

        public new class UxmlFactory : UxmlFactory<ManeuverNodeRow, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _isHeader = new() {name = "IsHeader", defaultValue = true};

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is ManeuverNodeRow row)
                {
                    if (_isHeader.GetValueFromBag(bag, cc))
                    {
                        row.NodeName.text = "Name";
                        row.UniversalTime.text = "UT";
                        row.DeltaV.text = "Δv";
                        row.GUID.text = "GUID";
                    }
                    else
                    {
                        row.NodeName.text = "Node-1";
                        row.UniversalTime.text = "1234.5";
                        row.DeltaV.text = "1234.5";
                        row.GUID.text = "abc-def-ghi";
                    }
                }
            }
        }
    }
}