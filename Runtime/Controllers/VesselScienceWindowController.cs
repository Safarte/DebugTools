using System.Collections.Generic;
using System.Linq;
using KSP.Game;
using KSP.Game.Science;
using KSP.Messages;
using KSP.Sim.impl;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class VesselScienceWindowController : BaseWindowController
    {
        private const string PhysXBubbleMaterialAddress = "Assets/Modules/DebugTools/Assets/UI/PhysXBubbleMat.mat";
        private const string OverflowTextClassName = "overflow-text";

        private DropdownField? _vessel;
        private Toggle? _useActive;

        private Label? _researchLocation;
        private Label? _scienceSituation;
        private Label? _scienceRegion;
        private Label? _scienceScalars;

        private Slider? _overlayStrength;
        private PQSScienceOverlay? _scienceOverlay;
        private Material? _regionBoundsMaterial;

        private Label? _hasStorageCapacity;

        private Button? _createReport;
        private Button? _submitReport;
        private Button? _transmitReport;
        private Button? _deleteReport;

        private ScrollView? _experimentIDsView;
        private List<string> _experimentIDs = new();

        private ScrollView? _reportsView;

        private readonly List<VesselComponent> _allVessels = new();
        private ScienceStorageComponent? _storageComponent;

        private void Awake()
        {
            Game.Messages.Subscribe<AddVesselToMapMessage>(OnVesselAdded);
            Game.Messages.Subscribe<VesselDestroyedMessage>(OnVesselRemoved);
            Game.Messages.Subscribe<GameLoadFinishedMessage>(OnGameLoaded);

            GameManager.Instance.Assets.Load<Material>(PhysXBubbleMaterialAddress, mat => _regionBoundsMaterial = mat);
        }

        private void OnDestroy()
        {
            Game.Messages.Unsubscribe<AddVesselToMapMessage>(OnVesselAdded);
            Game.Messages.Unsubscribe<VesselDestroyedMessage>(OnVesselRemoved);
            Game.Messages.Unsubscribe<GameLoadFinishedMessage>(OnGameLoaded);
        }

        private void OnEnable()
        {
            Enable();

            _vessel = RootElement.Q<DropdownField>("vessel");
            _useActive = RootElement.Q<Toggle>("use-active");
            _useActive.value = true;

            _researchLocation = RootElement.Q<Label>("research-location");
            _scienceSituation = RootElement.Q<Label>("situation");
            _scienceRegion = RootElement.Q<Label>("region");
            _scienceScalars = RootElement.Q<Label>("scalars");

            _overlayStrength = RootElement.Q<Slider>("overlay-strength");

            _hasStorageCapacity = RootElement.Q<Label>("has-storage-capacity");

            _createReport = RootElement.Q<Button>("create-report");
            _createReport.clicked += CreateTestReport;
            
            _submitReport = RootElement.Q<Button>("submit-report");
            _submitReport.clicked += SubmitReport;
            
            _transmitReport = RootElement.Q<Button>("transmit-report");
            _transmitReport.clicked += TransmitReport;
            
            _deleteReport = RootElement.Q<Button>("delete-reports");
            _deleteReport.clicked += DeleteReports;

            _experimentIDsView = RootElement.Q<ScrollView>("experiment-ids");
            _reportsView = RootElement.Q<ScrollView>("reports");
        }

        private void OnGameLoaded(MessageCenterMessage msg)
        {
            _allVessels.AddRange(Game.UniverseModel.GetAllVessels());
            _scienceOverlay = Window.gameObject.AddComponent<PQSScienceOverlay>();
            UpdateVesselDropdown();
            UpdateScienceMetadata();
        }

        private void UpdateScienceMetadata()
        {
            if (_experimentIDsView == null) return;

            _experimentIDsView.Clear();
            if (!Game.ScienceManager.ScienceExperimentsDataStore.IsInitialized) return;

            _experimentIDs = Game.ScienceManager.ScienceExperimentsDataStore.GetAllExperimentIDs();
            foreach (var id in _experimentIDs)
            {
                var label = new Label { text = id };
                label.AddToClassList(OverflowTextClassName);
                _experimentIDsView.Add(label);
            }
        }

        private void OnVesselAdded(MessageCenterMessage msg)
        {
            if (msg is AddVesselToMapMessage addVesselToMapMessage)
            {
                _allVessels.Add(addVesselToMapMessage.Vessel);
                UpdateVesselDropdown();
            }
        }

        private void OnVesselRemoved(MessageCenterMessage msg)
        {
            if (msg is VesselDestroyedMessage vesselDestroyedMessage)
            {
                _allVessels.Remove(vesselDestroyedMessage.Vessel);
                UpdateVesselDropdown();
            }
        }

        private void UpdateVesselDropdown()
        {
            _vessel!.choices.Clear();
            _vessel.choices = _allVessels.Select(p => p.DisplayName + " (" + p.mainBody.bodyName + ")").ToList();
        }

        private void LateUpdate()
        {
            if (!IsWindowOpen) return;

            if (!TryGetVessel(out var vessel) || vessel == null)
            {
                SetDefaultLabels();

                _experimentIDsView?.Clear();
                _reportsView?.Clear();

                _createReport?.SetEnabled(false);
                _submitReport?.SetEnabled(false);
                _transmitReport?.SetEnabled(false);
                _deleteReport?.SetEnabled(false);

                return;
            }

            // Update science situation labels
            var situation = vessel.VesselScienceRegionSituation;
            UpdateScienceSituationLabels(situation);

            // Update science biomes PQS overlay
            UpdateScienceOverlay(vessel);

            // Update research reports
            UpdateResearchReports(vessel);
        }

        private bool TryGetVessel(out VesselComponent? vessel)
        {
            vessel = null;

            if (_vessel == null || _useActive == null) return false;

            // Disable vessel
            _vessel.SetEnabled(!_useActive.value);

            if (_useActive.value)
            {
                vessel = Game.ViewController.GetActiveSimVessel();
                _vessel.index = _allVessels.IndexOf(vessel);
                return true;
            }

            if (_vessel.index < 0 || _vessel.index >= _allVessels.Count) return false;

            vessel = _allVessels[_vessel.index];
            return true;
        }

        private void SetDefaultLabels()
        {
            if (_researchLocation == null) return;
            _researchLocation.text = "Research Location:";

            if (_scienceSituation == null) return;
            _scienceSituation.text = "Situation:";

            if (_scienceRegion == null) return;
            _scienceRegion.text = "Region:";

            if (_scienceScalars == null) return;
            _scienceScalars.text = "Scalars (CB/Sit./Reg.):";

            if (_hasStorageCapacity == null) return;
            _hasStorageCapacity.text = "Has Research Report Storage Capacity:";
        }

        private void UpdateScienceSituationLabels(ScienceLocationRegionSituation situation)
        {
            if (_researchLocation == null || _scienceSituation == null || _scienceRegion == null ||
                _scienceScalars == null) return;

            // Research location ID (e.g. "Kerbin_Splashed_KerbinWater")
            _researchLocation.text = $"Research Location: <b>{situation.ResearchLocation.ResearchLocationId}</b>";

            // Science situation (e.g. "Splashed")
            var translatedSituation = situation.ResearchLocation.ScienceSituation.GetTranslatedDescription();
            var scienceSituation = situation.ResearchLocation.ScienceSituation;
            _scienceSituation.text = $"Situation: <b>{translatedSituation}</b> (<b>{scienceSituation}</b>)";

            // Science region (e.g. "KerbinWater")
            var translatedRegion = ScienceRegionsHelper.GetRegionDisplayName(situation.ResearchLocation.ScienceRegion);
            var scienceRegion = situation.ResearchLocation.ScienceRegion;
            _scienceRegion.text = $"Region: <b>{translatedRegion}</b> (<b>{scienceRegion}</b>)";

            // Science scalars (body/situation/region)
            _scienceScalars.text = $"Scalars (CB/Sit./Reg.): " +
                                   $"<b>{situation.CelestialBodyScalar}</b>/" +
                                   $"<b>{situation.SituationScalar}</b>/" +
                                   $"<b>{situation.ScienceRegionScalar}</b>";
        }

        private void UpdateScienceOverlay(VesselComponent vessel)
        {
            if (_scienceOverlay == null || _regionBoundsMaterial == null) return;

            var showOverlay = _overlayStrength?.value > 0 && vessel?.mainBody != null;
            if (showOverlay && Game?.ScienceManager?.ScienceRegionsDataProvider != null)
            {
                _scienceOverlay.SetScienceRegionsDataProvider(Game.ScienceManager.ScienceRegionsDataProvider);

                var celestialBodyBehavior = vessel!.mainBody.SurfaceProvider as CelestialBodyBehavior;
                var celestialBody = (celestialBodyBehavior != null) ? celestialBodyBehavior.PqsController : null;
                if (celestialBody != null)
                    _scienceOverlay.SetCelestialBody(celestialBody);

                _scienceOverlay.Strength = _overlayStrength!.value;
            }

            _scienceOverlay.enabled = showOverlay;
            PQSObject.RegionBoundingSphereMaterial = _regionBoundsMaterial;
            PQSObject.ShowScienceRegionInfo = showOverlay;
        }

        private void UpdateResearchReports(VesselComponent vessel)
        {
            if (_hasStorageCapacity == null || _reportsView == null) return;

            _hasStorageCapacity.text = "Has Research Report Storage Capability : " +
                                       (vessel.ReportStorageAllowed ? "<b>YES</b>" : "<b>NO</b>");

            _reportsView.Clear();
            if (vessel.ReportStorageAllowed &&
                vessel.SimulationObject.TryFindComponent<ScienceStorageComponent>(out _storageComponent))
            {
                foreach (var report in _storageComponent.GetStoredResearchReports())
                {
                    var label = new Label { text = $"{report.ResearchReportKey} {report.FlavorText}" };
                    label.AddToClassList(OverflowTextClassName);
                    _reportsView.Add(label);
                }
            }

            _createReport?.SetEnabled(true);
            _submitReport?.SetEnabled(true);
            _transmitReport?.SetEnabled(true);
            _deleteReport?.SetEnabled(true);
        }

        private void CreateTestReport()
        {
            if (_experimentIDs.Count <= 0 || _storageComponent == null) return;

            // Get active or selected vessel
            if (!TryGetVessel(out var vessel) || vessel == null) return;

            var foundValidReport = false;
            var retries = 0;
            while (!foundValidReport && retries < 100)
            {
                ++retries;

                // Choose random experiment on vessel
                var index = Random.Range(0, _experimentIDs.Count - 1);
                var definition =
                    Game.ScienceManager.ScienceExperimentsDataStore.GetExperimentDefinition(_experimentIDs[index]);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (definition == null) continue;

                var scienceReportType = definition.ExperimentType == ScienceExperimentType.SampleType
                    ? ScienceReportType.SampleType
                    : ScienceReportType.DataType;

                // Skip if current location is invalid for randomly selected experiment
                if (!definition.IsLocationValid(vessel.VesselScienceRegionSituation.ResearchLocation,
                        out var regionRequired)) continue;

                vessel.VesselScienceRegionSituation.ResearchLocation.RequiresRegion = regionRequired;

                // Experiment science amount
                var scienceAmount = definition.DataValue * vessel.VesselScienceRegionSituation.CelestialBodyScalar;
                scienceAmount *= vessel.VesselScienceRegionSituation.SituationScalar;
                scienceAmount *= vessel.VesselScienceRegionSituation.ScienceRegionScalar;

                // Report flavor text
                var flavorText = Game.ScienceManager.ScienceExperimentsDataStore.GetFlavorText(definition.ExperimentID,
                    vessel.VesselScienceRegionSituation.ResearchLocation.ResearchLocationId, scienceReportType);

                // Create and store report
                var researchReport = new ResearchReport(definition.ExperimentID, definition.DisplayName,
                    vessel.VesselScienceRegionSituation.ResearchLocation, scienceReportType, scienceAmount, flavorText);
                _storageComponent.StoreResearchReport(researchReport);

                foundValidReport = true;
            }
        }

        private void SubmitReport()
        {
            if (Game?.ScienceManager == null) return;
            
            var report = default(CompletedResearchReport);
            report.ExperimentID = IGGuid.NewGuid().ToString();
            report.ResearchLocationID = "Test";
            report.ResearchReportType = ScienceReportType.DataType;
            report.FinalScienceValue = 1000f;
            
            Game.ScienceManager.TrySubmitCompletedResearchReport(report);
        }

        private void TransmitReport()
        {
            if (_storageComponent == null || _storageComponent.GetNumberOfStoredResearchReports() <= 0) return;
            
            var storedReports = _storageComponent.GetStoredResearchReports();
            if (storedReports.Count <= 0) return;
                
            var report = storedReports[0];
            _storageComponent.StartReportTransmission(report.ResearchReportKey);
        }

        private void DeleteReports()
        {
            if (_experimentIDs.Count <= 0 || _storageComponent == null) return;
            
            var storedReports = _storageComponent.GetStoredResearchReports();
            foreach (var report in storedReports)
                _storageComponent.RemoveResearchReport(report.ResearchReportKey);
        }
    }
}