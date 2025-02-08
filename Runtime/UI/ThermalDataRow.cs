using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.UI
{
    public class ThermalDataRow : VisualElement
    {
        private const string ClassName = "data-row";
        private const string EntryClassName = "data-row-entry";
        private const string SmallEntryClassName = "data-row-entry-small";
        private const string LargeEntryClassName = "data-row-entry-large";

        public readonly Label PartName;
        public readonly Label ThermalMass;
        public readonly Label Temperature;
        public readonly Label WettedArea;
        public readonly Label ShockMult;
        public readonly Label ShockArea;
        public readonly Label ReentryFlux;
        public readonly Label EnvironmentFlux;
        public readonly Label OtherFlux;
        public readonly Label ExposedArea;
        public readonly Label ConeType;
        public readonly Label ShockAngle;
        public readonly Label ShockDistance;
        public readonly Label IsShielded;

        public ThermalDataRow()
        {
            AddToClassList(ClassName);

            PartName = new Label();
            PartName.AddToClassList(LargeEntryClassName);
            hierarchy.Add(PartName);

            ThermalMass = new Label();
            ThermalMass.AddToClassList(EntryClassName);
            hierarchy.Add(ThermalMass);

            Temperature = new Label();
            Temperature.AddToClassList(LargeEntryClassName);
            hierarchy.Add(Temperature);

            WettedArea = new Label();
            WettedArea.AddToClassList(EntryClassName);
            hierarchy.Add(WettedArea);

            ShockMult = new Label();
            ShockMult.AddToClassList(EntryClassName);
            hierarchy.Add(ShockMult);

            ShockArea = new Label();
            ShockArea.AddToClassList(EntryClassName);
            hierarchy.Add(ShockArea);

            ReentryFlux = new Label();
            ReentryFlux.AddToClassList(EntryClassName);
            hierarchy.Add(ReentryFlux);

            EnvironmentFlux = new Label();
            EnvironmentFlux.AddToClassList(EntryClassName);
            hierarchy.Add(EnvironmentFlux);

            OtherFlux = new Label();
            OtherFlux.AddToClassList(EntryClassName);
            hierarchy.Add(OtherFlux);

            ExposedArea = new Label();
            ExposedArea.AddToClassList(EntryClassName);
            hierarchy.Add(ExposedArea);

            ConeType = new Label();
            ConeType.AddToClassList(SmallEntryClassName);
            hierarchy.Add(ConeType);

            ShockAngle = new Label();
            ShockAngle.AddToClassList(SmallEntryClassName);
            hierarchy.Add(ShockAngle);

            ShockDistance = new Label();
            ShockDistance.AddToClassList(SmallEntryClassName);
            hierarchy.Add(ShockDistance);

            IsShielded = new Label();
            IsShielded.AddToClassList(SmallEntryClassName);
            hierarchy.Add(IsShielded);
        }

        public new class UxmlFactory : UxmlFactory<ThermalDataRow, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _isHeader = new() { name = "IsHeader", defaultValue = true };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is ThermalDataRow row)
                {
                    if (_isHeader.GetValueFromBag(bag, cc))
                    {
                        row.PartName.text = "Part";
                        row.ThermalMass.text = "M<sub>th</sub>";
                        row.Temperature.text = "T<sub>part</sub>/T<sub>max</sub>";
                        row.WettedArea.text = "A<sub>wet</sub>";
                        row.ShockMult.text = "p<sub>shock</sub>";
                        row.EnvironmentFlux.text = "Q<sub>env</sub>";
                        row.ReentryFlux.text = "Q<sub>reentry</sub>";
                        row.OtherFlux.text = "Q<sub>other</sub>";
                        row.ExposedArea.text = "A<sub>exp</sub>";
                        row.ShockArea.text = "A<sub>reentry</sub>";
                        row.ConeType.text = "C<sub>type</sub>";
                        row.ShockAngle.text = "Z<sub>shk</sub>";
                        row.ShockDistance.text = "D<sub>shk</sub>";
                        row.IsShielded.text = "Shield";
                    }
                    else
                    {
                        row.PartName.text = "Waow";
                        row.ThermalMass.text = "99.9J/K";
                        row.Temperature.text = "999K/9.99K";
                        row.WettedArea.text = "9.99m<sup>2</sup>";
                        row.ShockMult.text = "99.9%";
                        row.EnvironmentFlux.text = "999W";
                        row.ReentryFlux.text = "999W";
                        row.OtherFlux.text = "999W";
                        row.ExposedArea.text = "999m<sup>2</sup>";
                        row.ShockArea.text = "999m<sup>2</sup>";
                        row.ConeType.text = "Obl";
                        row.ShockAngle.text = "99°";
                        row.ShockDistance.text = "99";
                        row.IsShielded.text = "X";
                    }
                }
            }
        }
    }
}