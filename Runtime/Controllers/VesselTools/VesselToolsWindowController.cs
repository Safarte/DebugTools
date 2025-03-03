using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DebugTools.Utils;
using KSP.Game;
using KSP.Messages;
using KSP.Modules;
using KSP.Sim;
using KSP.Sim.impl;
using UnityEngine;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers.VesselTools
{
    public class VesselToolsWindowController : BaseWindowController
    {
        private bool _initialized;

        // Debug shapes prefabs
        private const string PrefabPath = "Assets/Modules/DebugTools/Assets/";

        private DebugShapesArrowComponent? _debugArrowPrefab;
        private DebugShapesAxesComponent? _debugAxesPrefab;

        // Stats windows
        private Toggle? _thermalDataToggle;
        private ThermalDataWindowController? _thermalData;
        private float _thermalDataLastUpdated = -1f;

        private Toggle? _massStatsToggle;
        private MassStatsWindowController? _massStats;
        private float _massStatsLastUpdated = -1f;

        private Toggle? _maneuversToggle;
        private ManeuverNodesWindowController? _maneuvers;
        private float _maneuversLastUpdated = -1f;

        private Toggle? _coordsToggle;
        private VesselCoordinatesWindowController? _coords;
        private float _coordsLastUpdated = -1f;

        // Flight axes
        private Toggle? _showControlPoints;
        private readonly List<DebugShapesAxesComponent> _vesselControlAxes = new();
        private bool _isControlPointsShowing;

        private Toggle? _cpNavballRotation;
        private readonly Quaternion _navballRotation = Quaternion.Euler(-90f, 0f, 0f);
        private bool _isControlPointsRotated = true;

        private Toggle? _showSASTargets;
        private readonly List<DebugShapesArrowComponent> _vesselSASArrows = new();
        private readonly Color _sasActiveColor = new(0.6f, 0f, 0.6f);
        private bool _isSASTargetsShowing;

        private Toggle? _showOrbitPoints;
        private readonly List<DebugShapesAxesComponent> _vesselOrbitAxes = new();
        private bool _isOrbitPointsShowing;

        // Joints
        private Toggle? _inertiaTensorScaling;
        private Toggle? _dynamicTensorSolution;
        private Toggle? _scaledSolverIteration;
        private Toggle? _multiJoints;
        private Toggle? _jointsEnabled;
        private Toggle? _showJoints;

        // Buoyancy
        private Toggle? _showPartsBounds;
        private Toggle? _showCoB;

        // Misc
        private Toggle? _showCoMMarkers;
        private readonly List<DebugShapesSphereMarker> _comMarkers = new();
        private Slider? _markerSize;

        private Toggle? _showPhysicsForce;
        private bool _isPhysicsForceShowing;

        private Toggle? _showInputValues;
        private VisualElement? _inputValues;
        private Label? _pitch;
        private Label? _yaw;
        private Label? _roll;

        private Label? _controlState;

        private GameState _state;
        private ViewController? _view;

        private List<VesselComponent> _vessels = new();

        private bool _ignoreValueChanged;

        private void Awake()
        {
            Game.Messages.Subscribe<GameStateChangedMessage>(OnStateChanged);
            Game.Messages.Subscribe<GameLoadFinishedMessage>(OnStateChanged);
            Refresh();
        }

        private void OnStateChanged(MessageCenterMessage msg)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!_initialized) return;

            _state = Game.GlobalGameState.GetGameState().GameState;
            _view = Game.ViewController;

            // Joints
            _ignoreValueChanged = true;
            _inertiaTensorScaling!.value = PhysicsSettings.ENABLE_INERTIA_TENSOR_SCALING;
            _dynamicTensorSolution!.value = PhysicsSettings.ENABLE_DYNAMIC_TENSOR_SOLUTION;
            _scaledSolverIteration!.value = PhysicsSettings.ENABLE_SCALED_SOLVER_ITERATION;
            _multiJoints!.value = PersistentProfileManager.MultiJointsEnabled;
            _jointsEnabled!.value = !PhysicsSettings.DEBUG_DISABLE_JOINTS;
            _ignoreValueChanged = false;

            _isPhysicsForceShowing = Game.PhysicsForceDisplaySystem.IsDisplayed;
            _showPhysicsForce!.value = _isPhysicsForceShowing;

            if (_state == GameState.FlightView || _state == GameState.Map3DView)
            {
                return;
            }

            // Cleanup stuff when not in flight
            DestroyControlOrientations();
            DestroySASTargets();
            DestroyOrbitOrientations();

            _showControlPoints!.value = false;
            _showSASTargets!.value = false;
            _showJoints!.value = false;
            _showCoMMarkers!.value = false;

            Module_Drag.ShowDragDebug = _isPhysicsForceShowing;
            Module_LiftingSurface.ShowPAMDebug = _isPhysicsForceShowing;
        }

        private void OnEnable()
        {
            Enable();

            LoadPrefabs();

            // Stats windows
            InitThermalData();
            InitMassStats();
            InitManeuvers();
            InitCoords();

            // Flight axes
            _showControlPoints = RootElement.Q<Toggle>("show-control-points");
            _showControlPoints.RegisterValueChangedCallback(OnShowControlPointsChanged);

            _cpNavballRotation = RootElement.Q<Toggle>("cp-follow-navball");
            _cpNavballRotation.value = _isControlPointsRotated;
            _cpNavballRotation.RegisterValueChangedCallback(OnCPNavballRotationChanged);

            _showSASTargets = RootElement.Q<Toggle>("show-sas");
            _showSASTargets.RegisterValueChangedCallback(OnShowSASTargetsChanged);

            _showOrbitPoints = RootElement.Q<Toggle>("show-orbit-points");
            _showOrbitPoints.RegisterValueChangedCallback(OnShowOrbitPointsChanged);

            // Joints
            _inertiaTensorScaling = RootElement.Q<Toggle>("inertia-tensor-scaling");
            _inertiaTensorScaling.RegisterValueChangedCallback(OnInertiaTensorScalingChanged);

            _dynamicTensorSolution = RootElement.Q<Toggle>("dynamic-tensor-solution");
            _dynamicTensorSolution.RegisterValueChangedCallback(OnDynamicTensorSolutionChanged);

            _scaledSolverIteration = RootElement.Q<Toggle>("scaled-solver-iteration");
            _scaledSolverIteration.RegisterValueChangedCallback(OnScaledSolverIterationChanged);

            _multiJoints = RootElement.Q<Toggle>("multi-joints");
            _multiJoints.RegisterValueChangedCallback(OnMultiJointsChanged);

            _jointsEnabled = RootElement.Q<Toggle>("joints-enabled");
            _jointsEnabled.RegisterValueChangedCallback(OnJointsEnabledChanged);

            _showJoints = RootElement.Q<Toggle>("show-joints");

            // Buoyancy
            _showPartsBounds = RootElement.Q<Toggle>("parts-bounds");
            _showPartsBounds.RegisterValueChangedCallback(OnShowPartsBoundsChanged);

            _showCoB = RootElement.Q<Toggle>("show-cob");
            _showCoB.RegisterValueChangedCallback(OnShowCoBChanged);

            // Misc
            _showCoMMarkers = RootElement.Q<Toggle>("show-com-markers");

            _markerSize = RootElement.Q<Slider>("com-markers-size");

            _showPhysicsForce = RootElement.Q<Toggle>("show-physics-force");
            _showPhysicsForce.RegisterValueChangedCallback(OnShowPhysicsForceChanged);

            _showInputValues = RootElement.Q<Toggle>("show-input-values");
            _showInputValues.RegisterValueChangedCallback(OnShowInputValuesChanged);

            _inputValues = RootElement.Q<VisualElement>("input-values");
            _pitch = RootElement.Q<Label>("pitch");
            _yaw = RootElement.Q<Label>("yaw");
            _roll = RootElement.Q<Label>("roll");

            _controlState = RootElement.Q<Label>("control-state");

            _initialized = true;
        }

        private void LoadPrefabs()
        {
            GameManager.Instance.Assets.Load<GameObject>(PrefabPath + "DebugArrow.prefab",
                obj => { _debugArrowPrefab = obj.GetComponent<DebugShapesArrowComponent>(); });

            GameManager.Instance.Assets.Load<GameObject>(PrefabPath + "DebugAxes.prefab",
                obj => { _debugAxesPrefab = obj.GetComponent<DebugShapesAxesComponent>(); });
        }

        private void InitThermalData()
        {
            _thermalDataToggle = RootElement.Q<Toggle>("thermal-data-toggle");

            UITKHelper.LoadUxml("ThermalDataWindow", uxml =>
            {
                var window = UITKHelper.CreateWindowFromUxml(uxml, "ThermalDataWindow");
                _thermalData = window.gameObject.AddComponent<ThermalDataWindowController>();
                _thermalData.IsWindowOpen = false;

                _thermalDataToggle!.RegisterValueChangedCallback(evt => _thermalData.IsWindowOpen = evt.newValue);

                _thermalData.CloseButton.clicked += () =>
                {
                    _thermalData.IsWindowOpen = false;
                    _thermalDataToggle!.value = false;
                };
            });
        }

        private void InitMassStats()
        {
            _massStatsToggle = RootElement.Q<Toggle>("mass-stats-toggle");

            UITKHelper.LoadUxml("MassStatsWindow", uxml =>
            {
                var window = UITKHelper.CreateWindowFromUxml(uxml, "MassStatsWindow");
                _massStats = window.gameObject.AddComponent<MassStatsWindowController>();
                _massStats.IsWindowOpen = false;

                _massStatsToggle!.RegisterValueChangedCallback(evt => _massStats.IsWindowOpen = evt.newValue);

                _massStats.CloseButton.clicked += () =>
                {
                    _massStats.IsWindowOpen = false;
                    _massStatsToggle!.value = false;
                };
            });
        }

        private void InitManeuvers()
        {
            _maneuversToggle = RootElement.Q<Toggle>("maneuvers-toggle");

            UITKHelper.LoadUxml("ManeuversWindow", uxml =>
            {
                var window = UITKHelper.CreateWindowFromUxml(uxml, "ManeuversWindow");
                _maneuvers = window.gameObject.AddComponent<ManeuverNodesWindowController>();
                _maneuvers.IsWindowOpen = false;

                _maneuversToggle!.RegisterValueChangedCallback(evt => _maneuvers.IsWindowOpen = evt.newValue);

                _maneuvers.CloseButton.clicked += () =>
                {
                    _maneuvers.IsWindowOpen = false;
                    _maneuversToggle!.value = false;
                };
            });
        }

        private void InitCoords()
        {
            _coordsToggle = RootElement.Q<Toggle>("coords-toggle");

            UITKHelper.LoadUxml("VesselCoordinatesWindow", uxml =>
            {
                var window = UITKHelper.CreateWindowFromUxml(uxml, "VesselCoordinatesWindow");
                _coords = window.gameObject.AddComponent<VesselCoordinatesWindowController>();
                _coords.IsWindowOpen = false;

                _coordsToggle!.RegisterValueChangedCallback(evt => _coords.IsWindowOpen = evt.newValue);

                _coords.CloseButton.clicked += () =>
                {
                    _coords.IsWindowOpen = false;
                    _coordsToggle!.value = false;
                };
            });
        }

        private void LateUpdate()
        {
            UpdateThermalData();
            UpdateMassStats();
            UpdateManeuvers();
            UpdateCoords();
            
            if (!IsWindowOpen) return;
            
            UpdateJointVisualizations();
            UpdateVesselsCoM();

            if (_state == GameState.FlightView || _state == GameState.Map3DView)
            {
                UpdateControlStateValues();

                if (_showInputValues != null && _showInputValues.value)
                    UpdateInputValues();
            }
        }

        private void UpdateControlStateValues()
        {
            if (_view == null || _controlState == null) return;

            var vessel = _view.GetActiveSimVessel();
            _controlState.text = "Control State: ";
            _controlState.text += vessel != null ? vessel.ControlStatus.ToString() : "";
        }

        private void UpdateInputValues()
        {
            if (_view == null || _pitch == null || _yaw == null || _roll == null) return;

            _pitch.text = "Pitch: ";
            _yaw.text = "Yaw: ";
            _roll.text = "Roll: ";

            var vessel = _view.GetActiveSimVessel();
            var behavior = _view.GetBehaviorIfLoaded(vessel);
            if (behavior != null)
            {
                _pitch.text += behavior.flightCtrlState.pitch.ToString("0.00");
                _yaw.text += behavior.flightCtrlState.yaw.ToString("0.00");
                _roll.text += behavior.flightCtrlState.roll.ToString("0.00");
            }
            else
            {
                _pitch.text += "-";
                _yaw.text += "-";
                _roll.text += "-";
            }
        }

        private void UpdateThermalData()
        {
            if (_thermalData == null || !_thermalData.IsWindowOpen || _view == null) return;

            // Update mass stats every 2 seconds
            _thermalDataLastUpdated -= Time.unscaledDeltaTime;
            if (_thermalDataLastUpdated >= 0f) return;
            _thermalDataLastUpdated = 1f;

            var activeVessel = _view.GetActiveSimVessel();
            var activeBehavior = _view.GetBehaviorIfLoaded(activeVessel);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null || activeBehavior == null) return;

            _thermalData.SyncTo(activeVessel, activeBehavior);
        }

        private void UpdateMassStats()
        {
            if (_massStats == null || !_massStats.IsWindowOpen || _view == null) return;

            // Update mass stats every 2 seconds
            _massStatsLastUpdated -= Time.unscaledDeltaTime;
            if (_massStatsLastUpdated >= 0f) return;
            _massStatsLastUpdated = 1f;

            var activeVessel = _view.GetActiveSimVessel();
            var activeBehavior = _view.GetBehaviorIfLoaded(activeVessel);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null || activeBehavior == null) return;

            _massStats.SyncTo(activeVessel, activeBehavior);
        }

        private void UpdateManeuvers()
        {
            if (_maneuvers == null || !_maneuvers.IsWindowOpen || _view == null) return;

            // Update maneuvers every second
            _maneuversLastUpdated -= Time.unscaledDeltaTime;
            if (_maneuversLastUpdated >= 0f) return;
            _maneuversLastUpdated = 1f;

            var activeVessel = _view.GetActiveSimVessel();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null) return;

            _maneuvers.SyncTo(activeVessel);
        }

        private void UpdateCoords()
        {
            if (_coords == null || !_coords.IsWindowOpen || _view == null) return;

            // Update maneuvers every second
            _coordsLastUpdated -= Time.unscaledDeltaTime;
            if (_coordsLastUpdated >= 0f) return;
            _coordsLastUpdated = 0.5f;

            var activeVessel = _view.GetActiveSimVessel();
            var activeBehavior = _view.GetBehaviorIfLoaded(activeVessel);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null || activeBehavior == null) return;

            _coords.SyncTo(activeVessel, activeBehavior);
        }

        private void OnShowControlPointsChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue == _isControlPointsShowing) return;

            if (evt.newValue)
                CreateControlOrientations();
            else
                DestroyControlOrientations();
        }

        private void CreateControlOrientations()
        {
            DestroyControlOrientations();
            _vesselControlAxes.Clear();

            if (_view == null) return;

            _isControlPointsShowing = true;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                var behavior = _view.GetBehaviorIfLoaded(vessel);
                if (behavior == null || _debugAxesPrefab == null) continue;

                var axes = Instantiate(_debugAxesPrefab.gameObject, behavior.transform)
                    .GetComponent<DebugShapesAxesComponent>();

                axes.arrowLineLength = 2f;
                if (axes.tracker != null)
                {
                    axes.tracker.RotationOffset = _isControlPointsRotated ? _navballRotation : Quaternion.identity;
                    axes.tracker.Setup(vessel.SimulationObject, "Control", true);
                    axes.tracker.OnUpdate += UpdateVesselControl;
                }

                _vesselControlAxes.Add(axes);
            }
        }

        private void DestroyControlOrientations()
        {
            var count = _vesselControlAxes.Count;
            while (count-- > 0)
            {
                _vesselControlAxes[count].tracker.OnUpdate -= UpdateVesselControl;

                if (_vesselControlAxes[count].gameObject != null)
                    Destroy(_vesselControlAxes[count].gameObject);

                _vesselControlAxes.RemoveAt(count);
            }

            _isControlPointsShowing = false;
        }

        private static void UpdateVesselControl(ITransformModel t, SimulationObjectModel o)
        {
            if (o?.Vessel != null)
            {
                t.UpdatePosition(o.Vessel.ControlTransform.Position);
                t.UpdateRotation(o.Vessel.ControlTransform.Rotation);
            }
        }

        private void OnCPNavballRotationChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue == _isControlPointsRotated) return;

            foreach (var axis in _vesselControlAxes)
                axis.tracker.RotationOffset = evt.newValue ? _navballRotation : Quaternion.identity;

            _isControlPointsRotated = evt.newValue;
        }

        private void OnShowSASTargetsChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue == _isSASTargetsShowing) return;

            if (evt.newValue)
                CreateSASTargets();
            else
                DestroySASTargets();
        }

        private void CreateSASTargets()
        {
            DestroySASTargets();
            _vesselSASArrows.Clear();

            if (_view == null) return;

            _isSASTargetsShowing = true;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                var behavior = _view.GetBehaviorIfLoaded(vessel);
                if (behavior == null || _debugArrowPrefab == null) continue;

                var arrow = Instantiate(_debugArrowPrefab.gameObject, behavior.transform)
                    .GetComponent<DebugShapesArrowComponent>();
                try
                {
                    arrow.color = _sasActiveColor;
                    arrow.arrowLine.Dashed = true;
                    arrow.arrowLine.DashSize = 0.5f;
                    arrow.lineLength = 4f;
                    if (arrow.tracker != null)
                    {
                        arrow.tracker.Setup(vessel.SimulationObject, "SAS", startTracking: true);
                        arrow.tracker.RotationOffset = _navballRotation;
                        arrow.tracker.OnUpdate += UpdateSASVectors;
                    }

                    _vesselSASArrows.Add(arrow);
                }
                catch
                {
                    if (arrow != null)
                        Destroy(arrow.gameObject);
                }
            }
        }

        private void DestroySASTargets()
        {
            var count = _vesselSASArrows.Count;
            while (count-- > 0)
            {
                _vesselSASArrows[count].tracker.OnUpdate -= UpdateSASVectors;

                if (_vesselSASArrows[count].gameObject != null)
                    Destroy(_vesselSASArrows[count].gameObject);

                _vesselSASArrows.RemoveAt(count);
            }

            _isSASTargetsShowing = false;
        }

        private void UpdateSASVectors(ITransformModel t, SimulationObjectModel o)
        {
            var vessel = o.Vessel;
            if (vessel?.Autopilot?.SAS == null) return;

            t.UpdatePosition(vessel.ControlTransform.Position);

            switch (vessel.speedMode)
            {
                case SpeedDisplayMode.Orbit:
                    UpdateSASOrbit(t, vessel, o.Telemetry);
                    break;
                case SpeedDisplayMode.Surface:
                    UpdateSASSurface(t, vessel, o.Telemetry);
                    break;
                case SpeedDisplayMode.Target:
                    UpdateSASTarget(t, vessel, o.Telemetry);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateSASOrbit(ITransformModel t, VesselComponent vessel, TelemetryComponent telemetry)
        {
            switch (vessel.Autopilot.AutopilotMode)
            {
                case AutopilotMode.StabilityAssist:
                    t.UpdateRotation(
                        Game.UniverseView.PhysicsSpace.PhysicsToRotation(vessel.Autopilot.SAS.LockedRotation));
                    break;
                case AutopilotMode.Prograde:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementNormal,
                        telemetry.OrbitMovementPrograde));
                    break;
                case AutopilotMode.Retrograde:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementAntiNormal,
                        telemetry.OrbitMovementRetrograde));
                    break;
                case AutopilotMode.Normal:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementRetrograde,
                        telemetry.OrbitMovementNormal));
                    break;
                case AutopilotMode.Antinormal:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementPrograde,
                        telemetry.OrbitMovementAntiNormal));
                    break;
                case AutopilotMode.RadialIn:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementPrograde,
                        telemetry.OrbitMovementRadialIn));
                    break;
                case AutopilotMode.RadialOut:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementRetrograde,
                        telemetry.OrbitMovementRadialOut));
                    break;
                case AutopilotMode.Target:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementNormal,
                        telemetry.TargetDirection));
                    break;
                case AutopilotMode.AntiTarget:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementNormal,
                        telemetry.AntiTargetDirection));
                    break;
                case AutopilotMode.Maneuver:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementNormal,
                        telemetry.ManeuverDirection));
                    break;
                case AutopilotMode.Navigation:
                case AutopilotMode.Autopilot:
                default:
                    break;
            }
        }

        private void UpdateSASSurface(ITransformModel t, VesselComponent vessel, TelemetryComponent telemetry)
        {
            switch (vessel.Autopilot.AutopilotMode)
            {
                case AutopilotMode.StabilityAssist:
                    t.UpdateRotation(
                        Game.UniverseView.PhysicsSpace.PhysicsToRotation(vessel.Autopilot.SAS.LockedRotation));
                    break;
                case AutopilotMode.Prograde:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementNormal,
                        telemetry.SurfaceMovementPrograde));
                    break;
                case AutopilotMode.Retrograde:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementAntiNormal,
                        telemetry.SurfaceMovementRetrograde));
                    break;
                case AutopilotMode.Normal:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonDown,
                        telemetry.HorizonNorth));
                    break;
                case AutopilotMode.Antinormal:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonUp,
                        telemetry.HorizonSouth));
                    break;
                case AutopilotMode.RadialIn:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonNorth,
                        telemetry.HorizonDown));
                    break;
                case AutopilotMode.RadialOut:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonSouth,
                        telemetry.HorizonUp));
                    break;
                case AutopilotMode.Target:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonNorth,
                        telemetry.TargetDirection));
                    break;
                case AutopilotMode.AntiTarget:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonNorth,
                        telemetry.AntiTargetDirection));
                    break;
                case AutopilotMode.Maneuver:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonNorth,
                        telemetry.ManeuverDirection));
                    break;
                case AutopilotMode.Navigation:
                case AutopilotMode.Autopilot:
                default:
                    break;
            }
        }

        private void UpdateSASTarget(ITransformModel t, VesselComponent vessel, TelemetryComponent telemetry)
        {
            switch (vessel.Autopilot.AutopilotMode)
            {
                case AutopilotMode.StabilityAssist:
                    t.UpdateRotation(
                        Game.UniverseView.PhysicsSpace.PhysicsToRotation(vessel.Autopilot.SAS.LockedRotation));
                    break;
                case AutopilotMode.Prograde:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementNormal,
                        telemetry.TargetPrograde));
                    break;
                case AutopilotMode.Retrograde:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.OrbitMovementAntiNormal,
                        telemetry.TargetRetrograde));
                    break;
                case AutopilotMode.Normal:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.TargetRetrograde,
                        telemetry.HorizonNorth));
                    break;
                case AutopilotMode.Antinormal:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.TargetPrograde,
                        telemetry.HorizonSouth));
                    break;
                case AutopilotMode.RadialIn:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.TargetPrograde,
                        telemetry.HorizonDown));
                    break;
                case AutopilotMode.RadialOut:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.TargetRetrograde,
                        telemetry.HorizonUp));
                    break;
                case AutopilotMode.Target:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonNorth,
                        telemetry.TargetDirection));
                    break;
                case AutopilotMode.AntiTarget:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonNorth,
                        telemetry.AntiTargetDirection));
                    break;
                case AutopilotMode.Maneuver:
                    t.UpdateRotation(Rotation.LookRotation(telemetry.HorizonNorth,
                        telemetry.ManeuverDirection));
                    break;
                case AutopilotMode.Navigation:
                case AutopilotMode.Autopilot:
                default:
                    break;
            }
        }

        private void OnShowOrbitPointsChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue == _isOrbitPointsShowing) return;

            if (evt.newValue)
                CreateOrbitOrientations();
            else
                DestroyOrbitOrientations();
        }

        private void CreateOrbitOrientations()
        {
            DestroyOrbitOrientations();
            _vesselOrbitAxes.Clear();

            if (_view == null) return;

            _isOrbitPointsShowing = true;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                var behavior = _view.GetBehaviorIfLoaded(vessel);
                if (!(behavior != null) || !(_debugAxesPrefab != null)) continue;

                var component = Instantiate(_debugAxesPrefab.gameObject, behavior.transform)
                    .GetComponent<DebugShapesAxesComponent>();
                try
                {
                    component.arrowLineLength = 2f;
                    component.forwardColor = Color.green;
                    component.upColor = Color.magenta;
                    component.rightColor = Color.cyan;
                    if (component.tracker != null)
                    {
                        component.tracker.Setup(vessel.SimulationObject, "Horizon", startTracking: true);
                        component.tracker.OnUpdate += Tracker_OnUpdate;
                        component.tracker.UpdateTransform();
                    }

                    _vesselOrbitAxes.Add(component);
                }
                catch
                {
                    if (component != null)
                        Destroy(component.gameObject);
                }
            }
        }

        private void DestroyOrbitOrientations()
        {
            var count = _vesselOrbitAxes.Count;
            while (count-- > 0)
            {
                _vesselOrbitAxes[count].tracker.OnUpdate -= Tracker_OnUpdate;

                if (_vesselOrbitAxes[count].gameObject != null)
                    Destroy(_vesselOrbitAxes[count].gameObject);

                _vesselOrbitAxes.RemoveAt(count);
            }

            _isOrbitPointsShowing = false;
        }

        private static void Tracker_OnUpdate(ITransformModel t, SimulationObjectModel o)
        {
            var vessel = o?.Vessel;
            if (vessel != null)
                t.UpdatePosition(vessel.CenterOfMass);

            var telemetry = o?.Telemetry;
            if (telemetry != null)
                t.UpdateRotation(telemetry.OrbitMovementRotation);
        }

        private void OnInertiaTensorScalingChanged(ChangeEvent<bool> evt)
        {
            if (!_ignoreValueChanged)
                PhysicsSettings.ENABLE_INERTIA_TENSOR_SCALING = evt.newValue;
        }

        private void OnDynamicTensorSolutionChanged(ChangeEvent<bool> evt)
        {
            if (!_ignoreValueChanged)
                PhysicsSettings.ENABLE_DYNAMIC_TENSOR_SOLUTION = evt.newValue;
        }

        private static IEnumerator CoroutineRebuildVessel(VesselBehavior vessel)
        {
            vessel.DebugForcePackVessel();
            yield return null;
            vessel.DebugForceUnpackVessel();
        }

        private void OnScaledSolverIterationChanged(ChangeEvent<bool> evt)
        {
            if (_ignoreValueChanged || _view == null) return;

            PhysicsSettings.ENABLE_SCALED_SOLVER_ITERATION = evt.newValue;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                if (Game.SpaceSimulation.TryGetViewObjectComponent<PartOwnerBehavior>(vessel.SimulationObject,
                        out var partOwner) &&
                    partOwner.ViewObject.Vessel.IsUnpacked())
                {
                    StartCoroutine(CoroutineRebuildVessel(partOwner.ViewObject.Vessel));
                }
            }
        }

        private void OnMultiJointsChanged(ChangeEvent<bool> evt)
        {
            if (_ignoreValueChanged || _view == null) return;

            PersistentProfileManager.MultiJointsEnabled = evt.newValue;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                if (Game.SpaceSimulation.TryGetViewObjectComponent<PartOwnerBehavior>(vessel.SimulationObject,
                        out var partOwner) &&
                    partOwner.ViewObject.Vessel.IsUnpacked())
                {
                    StartCoroutine(CoroutineRebuildVessel(partOwner.ViewObject.Vessel));
                }
            }
        }

        private void OnJointsEnabledChanged(ChangeEvent<bool> evt)
        {
            if (_ignoreValueChanged || _view == null) return;

            PhysicsSettings.DEBUG_DISABLE_JOINTS = !evt.newValue;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                if (Game.SpaceSimulation.TryGetViewObjectComponent<PartOwnerBehavior>(vessel.SimulationObject,
                        out var partOwner) &&
                    partOwner.ViewObject.Vessel.IsUnpacked())
                {
                    partOwner.ViewObject.Vessel.DebugForcePackVessel();
                }
            }

            Game.UniverseView.PhysicsSpace.FloatingOrigin.IsPendingForceSnap = true;
        }

        private void UpdateJointVisualizations()
        {
            if (_view == null || _showJoints == null || !_showJoints.value || _state != GameState.FlightView) return;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                if (Game.SpaceSimulation.TryGetViewObjectComponent<PartOwnerBehavior>(vessel.SimulationObject,
                        out var behavior))
                {
                    UpdateVesselJointsVisualizations(behavior);
                }
            }
        }

        private void UpdateVesselJointsVisualizations(PartOwnerBehavior behavior)
        {
            var connections = behavior.JointConnections.ToList();

            foreach (var connection in connections)
            {
                if (connection?.visualJoints == null || connection.visualJoints.Length == 0 ||
                    connection?.Joints == null)
                    continue;

                var num = 0;
                foreach (var joint in connection.Joints)
                {
                    var sphere = connection.visualJoints[num++];
                    if (sphere == null) continue;

                    sphere.enabled = _showJoints!.value;
                    if (sphere.enabled)
                        sphere.UpdatePosition(joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
                }
            }
        }

        private static void OnShowPartsBoundsChanged(ChangeEvent<bool> evt)
        {
            DebugVisualizer.RenderPartDragBounds = evt.newValue;
        }

        private void OnShowCoBChanged(ChangeEvent<bool> evt)
        {
            if (_view == null) return;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            foreach (var part in _view.GetBehaviorIfLoaded(vessel).parts)
                part.SetBuoyancyDebugForcePos(evt.newValue);
        }

        private void CreateCoMMarkers()
        {
            _comMarkers.Clear();

            if (_view == null) return;

            _vessels = _view.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                var markerObject = new GameObject(vessel.Name + "_CoM");
                markerObject.transform.parent = Window.transform;

                var marker = markerObject.AddComponent<DebugShapesSphereMarker>();
                marker.SetRadius(1f);
                marker.SetEnabled(_showCoMMarkers?.value ?? false);

                _comMarkers.Add(marker);
            }
        }

        private void UpdateVesselsCoM()
        {
            if (_view == null || _showCoMMarkers == null || _markerSize == null) return;

            if (_state != GameState.FlightView && _state != GameState.Launchpad && _state != GameState.Runway) return;

            _vessels = _view.Universe.GetAllVessels();

            if (_vessels.Count != _comMarkers.Count)
                CreateCoMMarkers();


            var i = 0;
            foreach (var vessel in _vessels)
            {
                _comMarkers[i].SetEnabled(_showCoMMarkers.value);
                _comMarkers[i].SetCenter(Game.UniverseView.PhysicsSpace.PositionToPhysics(vessel.CenterOfMass));
                _comMarkers[i].SetRadius(_markerSize.value);
                ++i;
            }
        }

        private void OnShowPhysicsForceChanged(ChangeEvent<bool> evt)
        {
            if (_isPhysicsForceShowing == evt.newValue) return;

            Game.PhysicsForceDisplaySystem.TogglePhysicsForceDisplay();
            Module_Drag.ShowDragDebug = evt.newValue;
            Module_LiftingSurface.ShowPAMDebug = evt.newValue;
            _isPhysicsForceShowing = Game.PhysicsForceDisplaySystem.IsDisplayed;
        }

        private void OnShowInputValuesChanged(ChangeEvent<bool> evt)
        {
            if (_inputValues == null) return;

            _inputValues.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}