using System.Collections.Generic;
using DebugTools.Utils;
using KSP.Game;
using KSP.Logging;
using KSP.Messages;
using KSP.Modules;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers.VesselTools
{
    public class VesselToolsWindowController : BaseWindowController
    {
        private bool _initialized = false;

        // Debug shapes prefabs
        private const string PrefabPath = "Assets/Modules/DebugTools/Assets/";

        private DebugShapesArrowComponent? _debugArrowPrefab;
        private DebugShapesAxesComponent? _debugAxesPrefab;
        private DebugShapesCenterOfMarker? _centerOfPrefab;

        // Stats windows
        private Toggle? _massStatsToggle;
        private MassStatsWindowController? _massStats;
        private float _massStatsLastUpdated = -1f;

        private Toggle? _maneuversToggle;
        private ManeuverNodesWindowController? _maneuvers;
        private float _maneuversLastUpdated = -1f;

        // Flight axes
        private Toggle? _showControlPoints;
        private readonly List<DebugShapesAxesComponent> _vesselControlAxes = new();
        private bool _isControlPointsShowing;

        private Toggle? _addNavballRotation;
        private readonly Quaternion _navballRotation = Quaternion.Euler(-90f, 0f, 0f);
        private bool _isControlPointsRotated = true;

        private Toggle? _showSASTargets;
        private bool _isSASTargetsShowing;

        private Toggle? _showOrbitPoints;
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

        private Toggle? _showPartsDepths;

        private Toggle? _showCoB;

        // Misc
        private Toggle? _showCoMMarkers;
        private readonly List<DebugShapesCenterOfMarker> _comMarkers = new();
        private bool _isCoMMarkersShowing;

        private Slider? _markerSize;

        private Toggle? _showPhysicsForce;
        private bool _isPhysicsForceShowing;

        private Toggle? _listInputValues;
        private Label? _pitch;
        private Label? _yaw;
        private Label? _roll;

        private Label? _controlState;

        private GameState _state;
        private ViewController? _view;
        private UniverseView? _universe;

        private List<VesselComponent> _vessels = new();

        private void Awake()
        {
            Game.Messages.Subscribe<GameStateChangedMessage>(OnStateChanged);
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
            _universe = Game.UniverseView;

            // Joints
            _inertiaTensorScaling!.value = PhysicsSettings.ENABLE_INERTIA_TENSOR_SCALING;
            _dynamicTensorSolution!.value = PhysicsSettings.ENABLE_DYNAMIC_TENSOR_SOLUTION;
            _scaledSolverIteration!.value = PhysicsSettings.ENABLE_SCALED_SOLVER_ITERATION;
            _multiJoints!.value = PersistentProfileManager.MultiJointsEnabled;
            _jointsEnabled!.value = !PhysicsSettings.DEBUG_DISABLE_JOINTS;

            _isPhysicsForceShowing = Game.PhysicsForceDisplaySystem.IsDisplayed;
            _showPhysicsForce!.value = _isPhysicsForceShowing;

            if (_state == GameState.FlightView || _state == GameState.Map3DView)
            {
                return;
            }

            // Cleanup stuff when not in flight
            DestroyCoMMarkers();

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

            InitMassStats();
            InitManeuvers();

            _showControlPoints = RootElement.Q<Toggle>("show-control-points");
            _showControlPoints.RegisterValueChangedCallback(OnShowControlPointsChanged);

            _addNavballRotation = RootElement.Q<Toggle>("cp-follow-navball");
            _addNavballRotation.value = _isControlPointsRotated;

            _showSASTargets = RootElement.Q<Toggle>("show-sas");

            _showOrbitPoints = RootElement.Q<Toggle>("show-orbit-points");

            _inertiaTensorScaling = RootElement.Q<Toggle>("inertia-tensor-scaling");

            _dynamicTensorSolution = RootElement.Q<Toggle>("dynamic-tensor-solution");

            _scaledSolverIteration = RootElement.Q<Toggle>("scaled-solver-iteration");

            _multiJoints = RootElement.Q<Toggle>("multi-joints");

            _jointsEnabled = RootElement.Q<Toggle>("joints-enabled");

            _showJoints = RootElement.Q<Toggle>("show-joints");

            _showPartsBounds = RootElement.Q<Toggle>("parts-bounds");

            _showPartsDepths = RootElement.Q<Toggle>("parts-depths");

            _showCoB = RootElement.Q<Toggle>("show-cob");

            _showCoMMarkers = RootElement.Q<Toggle>("show-com-markers");
            _showCoMMarkers.RegisterValueChangedCallback(OnShowCoMMarkersChanged);

            _markerSize = RootElement.Q<Slider>("com-marker-size");

            _showPhysicsForce = RootElement.Q<Toggle>("show-physics-force");

            _listInputValues = RootElement.Q<Toggle>("show-input-values");

            _pitch = RootElement.Q<Label>("pitch");
            _yaw = RootElement.Q<Label>("yaw");
            _roll = RootElement.Q<Label>("roll");

            _controlState = RootElement.Q<Label>("control-state");

            _initialized = true;
        }

        private void LoadPrefabs()
        {
            var arrowHandle = Addressables.LoadAssetAsync<GameObject>(PrefabPath + "DebugArrow.prefab");
            arrowHandle.Completed += res =>
            {
                if (res.Status != AsyncOperationStatus.Succeeded)
                {
                    DebugToolsPlugin.Logger.LogError("Failed to load DebugArrow.prefab");
                    return;
                }

                _debugArrowPrefab = res.Result.GetComponent<DebugShapesArrowComponent>();
            };

            var axesHandle = Addressables.LoadAssetAsync<GameObject>(PrefabPath + "DebugAxes.prefab");
            axesHandle.Completed += res =>
            {
                if (res.Status != AsyncOperationStatus.Succeeded)
                {
                    DebugToolsPlugin.Logger.LogError("Failed to load DebugAxes.prefab");
                    return;
                }

                _debugAxesPrefab = res.Result.GetComponent<DebugShapesAxesComponent>();
            };

            var coHandle = Addressables.LoadAssetAsync<GameObject>(PrefabPath + "CenterOf.prefab");
            coHandle.Completed += res =>
            {
                if (res.Status != AsyncOperationStatus.Succeeded)
                {
                    DebugToolsPlugin.Logger.LogError("Failed to load CenterOf.prefab");
                    return;
                }

                _centerOfPrefab = res.Result.GetComponent<DebugShapesCenterOfMarker>();
            };
        }

        private void InitMassStats()
        {
            _massStatsToggle = RootElement.Q<Toggle>("mass-stats-toggle");

            var handle = UITKHelper.LoadUxml("MassStatsWindow");
            handle.Completed += resHandle =>
            {
                if (resHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    DebugToolsPlugin.Logger.LogError("Failed to load MassStatsWindow.uxml");
                    return;
                }

                var window = UITKHelper.CreateWindowFromUxml(resHandle.Result, "MassStatsWindow");
                _massStats = window.gameObject.AddComponent<MassStatsWindowController>();
                _massStats.IsWindowOpen = false;

                _massStatsToggle!.RegisterValueChangedCallback(evt => _massStats.IsWindowOpen = evt.newValue);

                _massStats.CloseButton.clicked += () =>
                {
                    _massStats.IsWindowOpen = false;
                    _massStatsToggle!.value = false;
                };
            };
        }

        private void InitManeuvers()
        {
            _maneuversToggle = RootElement.Q<Toggle>("maneuvers-toggle");

            var handle = UITKHelper.LoadUxml("ManeuversWindow");
            handle.Completed += resHandle =>
            {
                if (resHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    DebugToolsPlugin.Logger.LogError("Failed to load ManeuversWindow.uxml");
                    return;
                }

                var window = UITKHelper.CreateWindowFromUxml(resHandle.Result, "ManeuversWindow");
                _maneuvers = window.gameObject.AddComponent<ManeuverNodesWindowController>();
                _maneuvers.IsWindowOpen = false;

                _maneuversToggle!.RegisterValueChangedCallback(evt => _maneuvers.IsWindowOpen = evt.newValue);

                _maneuvers.CloseButton.clicked += () =>
                {
                    _maneuvers.IsWindowOpen = false;
                    _maneuversToggle!.value = false;
                };
            };
        }

        private void LateUpdate()
        {
            if (!IsWindowOpen) return;

            UpdateMassStats();
            UpdateManeuvers();

            if (_state == GameState.FlightView || _state == GameState.Map3DView)
            {
                UpdateControlStateValues();
            }
        }

        private void UpdateControlStateValues()
        {
            var vessel = Game.ViewController.GetActiveSimVessel();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_controlState != null)
            {
                _controlState.text = "Control State: ";
                _controlState.text += vessel != null ? vessel.ControlStatus.ToString() : "";
            }
        }

        private void UpdateMassStats()
        {
            if (_massStats == null || !_massStats.IsWindowOpen) return;

            // Update mass stats every 2 seconds
            _massStatsLastUpdated -= Time.unscaledTime;
            if (_massStatsLastUpdated >= 0f) return;
            _massStatsLastUpdated = 2f;

            var activeVessel = Game.ViewController.GetActiveSimVessel();
            var activeBehavior = Game.ViewController.GetBehaviorIfLoaded(activeVessel);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null || activeBehavior == null) return;

            _massStats.SyncTo(activeVessel, activeBehavior);
        }

        private void UpdateManeuvers()
        {
            if (_maneuvers == null || !_maneuvers.IsWindowOpen) return;

            // Update maneuvers every second
            _maneuversLastUpdated -= Time.unscaledTime;
            if (_maneuversLastUpdated >= 0f) return;
            _maneuversLastUpdated = 1f;

            var activeVessel = Game.ViewController.GetActiveSimVessel();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null) return;

            _maneuvers.SyncTo(activeVessel);
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
            _isControlPointsShowing = true;
            _vesselControlAxes.Clear();

            if (_view == null) return;

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
                {
                    Destroy(_vesselControlAxes[count].gameObject);
                }

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

        private void OnShowCoMMarkersChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue == _isCoMMarkersShowing) return;

            if (evt.newValue)
                CreateCoMMarkers();
            else
                DestroyCoMMarkers();
        }

        private void CreateCoMMarkers()
        {
            DestroyCoMMarkers();
            _comMarkers.Clear();

            _isCoMMarkersShowing = true;

            _vessels = Game.ViewController.Universe.GetAllVessels();
            foreach (var vessel in _vessels)
            {
                var behavior = Game.ViewController.GetBehaviorIfLoaded(vessel);

                if (behavior == null || _centerOfPrefab == null) continue;

                var marker = Instantiate(_centerOfPrefab.gameObject, behavior.transform)
                    .GetComponent<DebugShapesCenterOfMarker>();
                marker.transform.localScale = 3 * Vector3.one;

                try
                {
                    marker.Color = Color.yellow;
                    if (marker.tracker != null)
                    {
                        marker.tracker.Setup(vessel.SimulationObject, "CoM", true);
                        marker.tracker.OnUpdate += UpdateVesselCoM;
                    }

                    _comMarkers.Add(marker);
                }
                catch
                {
                    if (marker != null)
                        Destroy(marker.gameObject);
                }
            }
        }

        private void DestroyCoMMarkers()
        {
            var count = _comMarkers.Count;
            while (count-- > 0)
            {
                _comMarkers[count].tracker.OnUpdate -= UpdateVesselCoM;
                if (_comMarkers[count]?.gameObject != null)
                {
                    Destroy(_comMarkers[count].gameObject);
                }

                _comMarkers.RemoveAt(count);
            }

            _isCoMMarkersShowing = false;
        }

        private static void UpdateVesselCoM(ITransformModel t, SimulationObjectModel o)
        {
            if (o?.Vessel != null)
            {
                t.UpdatePosition(o.Vessel.CenterOfMass);
            }
        }
    }
}