using System;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.UI
{
    public class KerbalRosterRow : VisualElement
    {
        private const string ClassName = "kerbal-roster-row";
        private const string KerbalNameClassName = ClassName + "__kerbal-name";
        private const string SimObjectClassName = ClassName + "__sim-object";
        private const string SeatClassName = ClassName + "__seat";
        private const string EnrollmentDateClassName = ClassName + "__enrollment-date";
        private const string DeleteButtonClassName = ClassName + "__delete-button";
        
        public readonly Label KerbalName;
        public readonly Label SimObject;
        public readonly Label Seat;
        public readonly Label EnrollmentDate;

        private readonly Button _deleteButton;

        public Action<string> OnDelete;
        
        public KerbalRosterRow()
        {
            AddToClassList(ClassName);
            
            KerbalName = new Label();
            KerbalName.AddToClassList(KerbalNameClassName);
            hierarchy.Add(KerbalName);
            
            SimObject = new Label();
            SimObject.AddToClassList(SimObjectClassName);
            hierarchy.Add(SimObject);
            
            Seat = new Label();
            Seat.AddToClassList(SeatClassName);
            hierarchy.Add(Seat);
            
            EnrollmentDate = new Label();
            EnrollmentDate.AddToClassList(EnrollmentDateClassName);
            hierarchy.Add(EnrollmentDate);
            
            _deleteButton = new Button();
            _deleteButton.text = "Del";
            _deleteButton.AddToClassList(DeleteButtonClassName);
            _deleteButton.clicked += () => OnDelete?.Invoke(KerbalName.text);
            hierarchy.Add(_deleteButton);
        }
        
        public KerbalRosterRow(bool isHeader) : this()
        {
            if (!isHeader) return;
            
            KerbalName.text = "<b>Name</b>";
            SimObject.text = "<b>Sim Object</b>";
            Seat.text = "<b>Seat</b>";
            EnrollmentDate.text = "<b>Enrolled</b>";
            _deleteButton.visible = false;
        }
        
        public new class UxmlFactory : UxmlFactory<KerbalRosterRow, UxmlTraits>
        {
        }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _isHeader = new() { name = "IsHeader", defaultValue = true };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is KerbalRosterRow row)
                {
                    if (_isHeader.GetValueFromBag(bag, cc))
                    {
                        row.KerbalName.text = "<b>Name</b>";
                        row.SimObject.text = "<b>Sim Object</b>";
                        row.Seat.text = "<b>Seat</b>";
                        row.EnrollmentDate.text = "<b>Enrolled</b>";
                        row._deleteButton.visible = false;
                    }
                    else
                    {
                        row.KerbalName.text = "Valentina Kerman";
                        row.SimObject.text = "Kerbal 1X";
                        row.Seat.text = "42";
                        row.EnrollmentDate.text = "123456789";
                    }
                }
            }
        }
    }
}