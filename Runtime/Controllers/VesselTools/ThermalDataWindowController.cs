using System.Collections.Generic;
using System.Linq;
using DebugTools.Runtime.UI;
using KSP;
using KSP.Messages;
using KSP.Modules;
using KSP.Sim.impl;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers.VesselTools
{
    public class ThermalDataWindowController : BaseWindowController
    {
        // The elements of the window that we need to access
        private ScrollView? _itemsView;
        private List<ThermalDataRow>? _rows;
        
        private void OnEnable()
        {
            Enable();
            
            _rows = new List<ThermalDataRow>();

            _itemsView = RootElement.Q<ScrollView>("rows-view");
            _itemsView.Clear();
            var row = new ThermalDataRow(true);
            _itemsView.Add(row);
        }

        public void SyncTo(VesselComponent vessel, VesselBehavior behavior)
        {
            if (_itemsView == null || _rows == null) return;

            var parts = behavior.parts.ToList();

            if (parts.Count != _rows.Count)
            {
                _itemsView.Clear();
                _rows.Clear();
                
                var row = new ThermalDataRow(true);
                _itemsView.Add(row);

                foreach (var part in behavior.parts)
                {
                    row = new ThermalDataRow();
                    FormatThermalData(row, part);
                    _itemsView.Add(row);
                    _rows.Add(row);
                }

                return;
            }

            var i = 0;
            foreach (var part in behavior.parts)
                FormatThermalData(_rows[i++], part);
        }

        private static void FormatThermalData(ThermalDataRow row, PartBehavior part)
        {
            row.PartName.text = part.GetDisplayName();
            row.ThermalMass.text = Units.PrintSI(part.Model.ThermalMass, "J/K");
            row.Temperature.text = Units.PrintSI(part.Model.Temperature, Units.SymbolKelvin, 2) +
                                   "/" + Units.PrintSI(part.Model.MaxTemp, Units.SymbolKelvin);
            row.EnvironmentFlux.text =
                Units.PrintSI(part.Model.ThermalData.EnvironmentFlux * 1000.0, "W");
            row.ReentryFlux.text =
                Units.PrintSI(part.Model.ThermalData.ReentryFlux * 1000.0, "W");
            row.OtherFlux.text = Units.PrintSI(part.Model.ThermalData.OtherFlux * 1000.0, "W");
            row.ShockMult.text =
                Units.PrintSI(part.Model.ThermalData.ExposedAreaProportion * 100.0, "%");
            row.WettedArea.text =
                Units.PrintSI(part.Model.WettedArea, Units.SymbolMetersSquared);
            part.Model.TryGetModuleData<PartComponentModule_Drag, Data_Drag>(out var data);
            if (data != null)
                row.ExposedArea.text = Units.PrintSI(part.Model.ThermalData.CubeDataArea,
                    Units.SymbolMetersSquared);
            row.ShockArea.text = Units.PrintSI(
                part.Model.ThermalData.CubeDataArea * part.Model.ThermalData.ExposedAreaProportion,
                Units.SymbolMetersSquared);
            row.ConeType.text = (part.Model.ThermalData.ConeType == "ObliqueShock") ? "Obl" : "Bow";
            row.ShockAngle.text =
                $"{(float)part.Model.ThermalData.ShockAngle * 57.29578f:N2}{Units.SymbolDegree}";
            row.ShockDistance.text = $"{part.Model.ThermalData.ShockDistance:N2}";
        }
    }
}