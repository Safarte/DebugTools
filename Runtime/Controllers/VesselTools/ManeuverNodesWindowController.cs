// ReSharper disable UseObjectOrCollectionInitializer

using System.Collections.Generic;
using DebugTools.Runtime.UI.VesselTools;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers.VesselTools
{
    public class ManeuverNodesWindowController : BaseWindowController
    {
        private ScrollView? _itemsView;
        private List<ManeuverNodeRow>? _rows;

        private void OnEnable()
        {
            Enable();

            _rows = new List<ManeuverNodeRow>();
            
            _itemsView = RootElement.Q<ScrollView>("items-view");
            _itemsView.Clear();
            var row = new ManeuverNodeRow(true);
            _itemsView.Add(row);
        }

        private static void FormatNodeData(ManeuverNodeRow row, ManeuverNodeData node)
        {
            row.NodeName.text = node.NodeName;
            row.UniversalTime.text = node.Time.ToString("0.0");
            row.DeltaV.text = node.BurnVector.ToString("0.0");
            row.GUID.text = node.NodeID.ToString();
        }

        public void SyncTo(VesselComponent vessel)
        {
            if (_itemsView == null || _rows == null) return;

            var nodes = vessel.SimulationObject.ManeuverPlan.GetNodes();

            if (nodes.Count != _rows.Count)
            {
                _itemsView.Clear();
                _rows.Clear();

                var row = new ManeuverNodeRow(true);
                _itemsView.Add(row);

                foreach (var node in nodes)
                {
                    row = new ManeuverNodeRow();
                    FormatNodeData(row, node);
                    _itemsView.Add(row);
                    _rows.Add(row);
                }

                return;
            }

            var i = 0;
            foreach (var node in nodes)
            {
                FormatNodeData(_rows[i], node);
                ++i;
            }
        }
    }
}