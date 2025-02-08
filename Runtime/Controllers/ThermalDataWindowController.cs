using System.Collections.Generic;
using DebugTools.Runtime.UI;
using KSP;
using KSP.Messages;
using KSP.Modules;
using KSP.Sim.impl;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class ThermalDataWindowController : BaseWindowController
    {
        // The elements of the window that we need to access
        private ScrollView _rowsView;
        private readonly List<ThermalDataRow> _dataRows = new();

        // Current active vessel tracking
        private VesselComponent _activeVessel;
        private bool _vesselChanged = true;

        private void Awake()
        {
            Game.Messages.Subscribe<VesselChangedMessage>(VesselChanged);
            Game.Messages.Subscribe<PartDestroyedByExplosionMessage>(VesselChanged);
            Game.Messages.Subscribe<VesselUndockedMessage>(VesselChanged);
            Game.Messages.Subscribe<DecoupleMessage>(VesselChanged);
        }

        private void OnDestroy()
        {
            Game.Messages.Unsubscribe<VesselChangedMessage>(VesselChanged);
            Game.Messages.Unsubscribe<PartDestroyedByExplosionMessage>(VesselChanged);
            Game.Messages.Unsubscribe<VesselUndockedMessage>(VesselChanged);
            Game.Messages.Unsubscribe<DecoupleMessage>(VesselChanged);
        }

        private void LateUpdate()
        {
            if (IsWindowOpen)
            {
                if (_vesselChanged && Game != null && Game?.ViewController != null)
                {
                    _activeVessel = Game.ViewController.GetActiveSimVessel();
                }

                if (_activeVessel != null && _vesselChanged)
                {
                    _vesselChanged = false;
                    PopulateWindow();
                }

                UpdateTempValues();
            }
        }

        private void PopulateWindow()
        {
            if (_activeVessel == null) return;

            _dataRows.Clear();
        
            // Title row
            var titleRow = new ThermalDataRow();
            titleRow.PartName.text = "Part";
            titleRow.ThermalMass.text = "M<sub>th</sub>";
            titleRow.Temperature.text = "T<sub>part</sub>/T<sub>max</sub>";
            titleRow.WettedArea.text = "A<sub>wet</sub>";
            titleRow.ShockMult.text = "p<sub>shock</sub>";
            titleRow.EnvironmentFlux.text = "Q<sub>env</sub>";
            titleRow.ReentryFlux.text = "Q<sub>reentry</sub>";
            titleRow.OtherFlux.text = "Q<sub>other</sub>";
            titleRow.ExposedArea.text = "A<sub>exp</sub>";
            titleRow.ShockArea.text = "A<sub>reentry</sub>";
            titleRow.ConeType.text = "C<sub>type</sub>";
            titleRow.ShockAngle.text = "Z<sub>shk</sub>";
            titleRow.ShockDistance.text = "D<sub>shk</sub>";
            titleRow.IsShielded.text = "Shield";
            _dataRows.Add(titleRow);

            // Parts rows
            foreach (var part in Game.ViewController.GetBehaviorIfLoaded(_activeVessel).parts)
            {
                var row = new ThermalDataRow();
                part.Model.TryGetModuleData<PartComponentModule_Drag, Data_Drag>(out var data);
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
                _dataRows.Add(row);
            }

            _rowsView.Clear();
            foreach (var row in _dataRows)
                _rowsView.Add(row);
        }

        private void UpdateTempValues()
        {
            if (_activeVessel == null) return;

            var behaviorIfLoaded = Game.ViewController.GetBehaviorIfLoaded(_activeVessel);
            if (behaviorIfLoaded == null) return;

            var num = behaviorIfLoaded.parts.Count();
            foreach (var part in behaviorIfLoaded.parts)
            {
                var row = _dataRows[num];
                part.Model.TryGetModuleData<PartComponentModule_Drag, Data_Drag>(out var data);

                row.PartName.text = part.GetDisplayName();
                row.Temperature.text =
                    Units.PrintSI(part.Model.Temperature, Units.SymbolKelvin, 2) + "/" +
                    Units.PrintSI(part.Model.MaxTemp, Units.SymbolKelvin);
                row.ThermalMass.text = Units.PrintSI(part.Model.ThermalMass, "J/K");
                row.EnvironmentFlux.text =
                    Units.PrintSI(part.Model.ThermalData.EnvironmentFlux * 1000.0, "W");
                row.ReentryFlux.text =
                    Units.PrintSI(part.Model.ThermalData.ReentryFlux * 1000.0, "W");
                row.OtherFlux.text =
                    Units.PrintSI(part.Model.ThermalData.OtherFlux * 1000.0, "W");
                row.ShockMult.text =
                    Units.PrintSI(part.Model.ThermalData.ExposedAreaProportion * 100.0, "%");
                row.WettedArea.text =
                    Units.PrintSI(part.Model.WettedArea, Units.SymbolMetersSquared);
                if (data != null)
                    row.ExposedArea.text = Units.PrintSI(part.Model.ThermalData.CubeDataArea,
                        Units.SymbolMetersSquared);
                row.ShockArea.text = Units.PrintSI(
                    part.Model.ThermalData.CubeDataArea * part.Model.ThermalData.ExposedAreaProportion,
                    Units.SymbolMetersSquared);
                row.ConeType.text = (part.Model.ThermalData.ConeType == "ObliqueShock") ? "Obl" : "Bow";
                row.ShockAngle.text =
                    $"{(float)part.Model.ThermalData.ShockAngle * 57.29578f:N1}{Units.SymbolDegree}";
                row.ShockDistance.text = $"{part.Model.ThermalData.ShockDistance:N1}";
                row.IsShielded.text = part.ShieldedFromAirstream ? "X" : " ";

                if (--num <= 0)
                    break;
            }
        }

        /// <summary>
        /// Runs when the window is first created, and every time the window is re-enabled.
        /// </summary>
        private void OnEnable()
        {
            Enable();

            _rowsView = RootElement.Q<ScrollView>("rows-view");

            PopulateWindow();
        }
    
        private void VesselChanged(MessageCenterMessage msg)
        {
            _vesselChanged = true;
        }
    }
}