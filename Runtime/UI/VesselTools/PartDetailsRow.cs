using UnityEngine.UIElements;

namespace DebugTools.Runtime.UI.VesselTools
{
    public class PartDetailsRow : VisualElement
    {
        private const string ClassName = "part-details";
        private const string PartNameClassName = ClassName + "__name";
        private const string SubItemClassName = ClassName + "__item";

        public readonly Label PartName;
        public readonly Label ModelMass;
        public readonly Label PartMass;
        public readonly Label ResourceMass;
        public readonly Label GreenMass;
        public readonly Label RBMass;
        public readonly Label RBPhysXMass;
        public readonly Label WettedArea;

        public PartDetailsRow()
        {
            AddToClassList(ClassName);

            PartName = new Label();
            PartName.AddToClassList(PartNameClassName);
            hierarchy.Add(PartName);

            ModelMass = new Label();
            ModelMass.AddToClassList(SubItemClassName);
            hierarchy.Add(ModelMass);

            PartMass = new Label();
            PartMass.AddToClassList(SubItemClassName);
            hierarchy.Add(PartMass);

            ResourceMass = new Label();
            ResourceMass.AddToClassList(SubItemClassName);
            hierarchy.Add(ResourceMass);

            GreenMass = new Label();
            GreenMass.AddToClassList(SubItemClassName);
            hierarchy.Add(GreenMass);

            RBMass = new Label();
            RBMass.AddToClassList(SubItemClassName);
            hierarchy.Add(RBMass);

            RBPhysXMass = new Label();
            RBPhysXMass.AddToClassList(SubItemClassName);
            hierarchy.Add(RBPhysXMass);

            WettedArea = new Label();
            WettedArea.AddToClassList(SubItemClassName);
            hierarchy.Add(WettedArea);
        }

        public PartDetailsRow(bool isHeader) : this()
        {
            if (!isHeader) return;
            
            PartName.text = "Name";
            ModelMass.text = "M<sub>model</sub>";
            PartMass.text = "M<sub>part</sub>";
            ResourceMass.text = "M<sub>resource</sub>";
            GreenMass.text = "M<sub>green</sub>";
            RBMass.text = "M<sub>rb</sub>";
            RBPhysXMass.text = "M<sub>rbphysx</sub>";
            WettedArea.text = "A<sub>wet</sub>";
        }

        public new class UxmlFactory : UxmlFactory<PartDetailsRow, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _isHeader = new() {name = "IsHeader", defaultValue = true};

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is PartDetailsRow row)
                {
                    if (_isHeader.GetValueFromBag(bag, cc))
                    {
                        row.PartName.text = "Name";
                        row.ModelMass.text = "M<sub>model</sub>";
                        row.PartMass.text = "M<sub>part</sub>";
                        row.ResourceMass.text = "M<sub>resource</sub>";
                        row.GreenMass.text = "M<sub>green</sub>";
                        row.RBMass.text = "M<sub>rb</sub>";
                        row.RBPhysXMass.text = "M<sub>rbphysx</sub>";
                        row.WettedArea.text = "A<sub>wet</sub>";
                    }
                    else
                    {
                        row.PartName.text = "Waow";
                        row.ModelMass.text = "99.99";
                        row.PartMass.text = "33.33";
                        row.ResourceMass.text = "33.33";
                        row.GreenMass.text = "33.33";
                        row.RBMass.text = "99.99";
                        row.RBPhysXMass.text = "99.99";
                        row.WettedArea.text = "9.99 m<sup>2</sup>";
                    }
                }
            }
        }
    }
}