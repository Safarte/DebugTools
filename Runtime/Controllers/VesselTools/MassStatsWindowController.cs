// ReSharper disable UseObjectOrCollectionInitializer

using System.Collections.Generic;
using DebugTools.Runtime.UI.VesselTools;
using KSP;
using KSP.Sim;
using KSP.Sim.impl;
using UniLinq;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers.VesselTools
{
    public class MassStatsWindowController : BaseWindowController
    {
        private ScrollView? _itemsView;
        private PartDetailsRow? _vesselRow;
        private List<PartDetailsRow>? _rows;

        private void OnEnable()
        {
            Enable();

            _rows = new List<PartDetailsRow>();
            
            _itemsView = RootElement.Q<ScrollView>("items-view");
            _itemsView.Clear();
            var row = new PartDetailsRow(true);
            _itemsView.Add(row);
            
            _vesselRow = new PartDetailsRow();
        }

        public void SyncTo(VesselComponent vessel, VesselBehavior behavior)
        {
            if (_itemsView == null || _rows == null || _vesselRow == null) return;

            var parts = behavior.parts.ToList();

            if (parts.Count != _rows.Count)
            {
                _itemsView.Clear();
                _rows.Clear();

                var row = new PartDetailsRow(true);
                _itemsView.Add(row);

                _vesselRow = new PartDetailsRow();
                _vesselRow.PartName.text = $"<b>{vessel.RevealDisplayName()}</b>";
                _vesselRow.ModelMass.text = $"<b>{vessel.totalMass:F2}</b>";
                _itemsView.Add(_vesselRow);
                
                foreach (var part in behavior.parts)
                {
                    row = new PartDetailsRow();
                    FormatMassStats(row, part);
                    _itemsView.Add(row);
                    _rows.Add(row);
                }

                return;
            }

            _vesselRow.PartName.text = $"<b>{vessel.RevealDisplayName()}</b>";
            _vesselRow.ModelMass.text = $"<b>{vessel.totalMass:F2}</b>";

            var i = 0;
            foreach (var part in behavior.parts)
                FormatMassStats(_rows[i++], part);
        }

        private static void FormatMassStats(PartDetailsRow row, PartBehavior part)
        {
            row.PartName.text = part.GetDisplayName();
            row.ModelMass.text = part.Model.PhysicsMass.ToString("0.00");
            row.PartMass.text = part.Model.DryMass.ToString("0.00");
            row.ResourceMass.text = part.Model.ResourceMass.ToString("0.00");
            row.GreenMass.text = part.Model.GreenMass.ToString("0.00");
            if (part.Rigidbody != null && part.Rigidbody.activeRigidBody != null)
            {
                row.RBMass.text = part.Rigidbody.mass.ToString("0.00");
                row.RBPhysXMass.text = part.Rigidbody.PhysicsMode == PartPhysicsModes.None
                    ? "0.00"
                    : part.Rigidbody.activeRigidBody.mass.ToString("0.00");
            }

            row.WettedArea.text = $"{part.Model.WettedArea:F2} {Units.SymbolMetersSquared}";
        }
    }
}