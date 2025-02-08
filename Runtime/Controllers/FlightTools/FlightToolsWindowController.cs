using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Game;
using KSP.Messages;
using KSP.Sim;
using KSP.Sim.impl;
using KSP.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers.FlightTools
{
    /// <summary>
    /// Controller for the FlightTools UI.
    /// </summary>
    public class FlightToolsWindowController : BaseWindowController
    {
        private enum ItemType
        {
            Vessel = 0,
            Part = 1,
            JointConnection = 2,
            CelestialBody = 3
        }

        // Settings Toggles
        private Toggle _toggleProximityLoadUnload;
        private Toggle _toggleCelestialBodyCollisionApprox;
        private Toggle _toggleHighlightItems;
        private Toggle _toggleActiveVesselOnly;

        // Item type selector
        private DropdownField _itemTypeDropdown;

        // Items view
        private ScrollView _itemsView;

        private List<SimObjectItemController> _simObjects;
        private List<JointConnectionItemController> _jointConnections;

        private ItemType _filterType;
        private bool _highlightItems;
        private bool _showActiveVesselOnly = true;

        private bool _forceRefresh;

        private void Awake()
        {
            Game.Messages.Subscribe<VesselChangedMessage>(OnActiveVesselChanged);
            Game.Messages.Subscribe<GameLoadFinishedMessage>(OnGameLoadFinished);
            _simObjects = new List<SimObjectItemController>();
            _jointConnections = new List<JointConnectionItemController>();
        }

        /// <summary>
        /// Runs when the window is first created, and every time the window is re-enabled.
        /// </summary>
        private void OnEnable()
        {
            Enable();

            _toggleProximityLoadUnload = RootElement.Q<Toggle>("proximity-load-unload");
            _toggleProximityLoadUnload.RegisterValueChangedCallback(OnProximityLoadUnloadChanged);

            _toggleCelestialBodyCollisionApprox = RootElement.Q<Toggle>("celestial-body-col-approx");
            _toggleCelestialBodyCollisionApprox.RegisterValueChangedCallback(
                OnCelestialBodyCollisionApproximationChanged);

            _toggleHighlightItems = RootElement.Q<Toggle>("highlight-items");
            _toggleHighlightItems.RegisterValueChangedCallback(OnHighlightItemsChanged);

            _toggleActiveVesselOnly = RootElement.Q<Toggle>("active-only");
            _toggleActiveVesselOnly.value = _showActiveVesselOnly;
            _toggleActiveVesselOnly.RegisterValueChangedCallback(OnActivateVesselOnlyChanged);

            _itemTypeDropdown = RootElement.Q<DropdownField>("item-type");
            _itemTypeDropdown.choices = Enum.GetNames(typeof(ItemType)).ToList();
            _itemTypeDropdown.value = ItemType.Vessel.ToString();
            _itemTypeDropdown.RegisterValueChangedCallback(OnItemTypeChanged);

            _itemsView = RootElement.Q<ScrollView>("items-view");

            _forceRefresh = true;
        }

        private void LateUpdate()
        {
            if (IsGameShuttingDown || !IsWindowOpen) return;

            var universeModel = Game?.UniverseModel;
            if (universeModel == null) return;

            if (universeModel.IsSimObjectCountDirty) _forceRefresh = true;

            _toggleProximityLoadUnload.value = Game?.UniverseView?.IsProximityLoadUnloadEnabled ?? false;
            _toggleCelestialBodyCollisionApprox.value =
                Game?.SpaceSimulation?.IsCelestialBodyCollisionApproximationEnabled ?? false;

            // Toggling "Active Vessel Only" does not make sense for CelestialBody
            _toggleActiveVesselOnly.SetEnabled(_filterType is not ItemType.CelestialBody);

            if (!_forceRefresh)
            {
                RefreshCurrentItems();
                return;
            }

            var activeIGGuid = default(IGGuid);
            if (Game?.ViewController != null && Game.ViewController.TryGetActiveVehicle(out var activeVehicle))
                activeIGGuid = activeVehicle.Guid;

            if (_filterType == ItemType.JointConnection)
            {
                RefreshJointConnections(activeIGGuid);
            }
            else
            {
                RefreshSimObjects(activeIGGuid);
            }

            _forceRefresh = false;
        }

        private void RefreshCurrentItems()
        {
            if (_filterType == ItemType.JointConnection)
            {
                foreach (var jointConnection in _jointConnections)
                    jointConnection.Refresh();

                return;
            }

            foreach (var simObject in _simObjects)
                simObject.Refresh();
        }

        private void RefreshJointConnections(IGGuid activeIGGuid)
        {
            _itemsView.Clear();
            _jointConnections.Clear();

            var universeModel = Game?.UniverseModel;
            if (universeModel == null) return;

            foreach (var simObject in universeModel.GetAllSimObjects())
            {
                if (simObject?.PartOwner == null ||
                    (_showActiveVesselOnly && (!_showActiveVesselOnly || !(activeIGGuid == simObject.GlobalId))) ||
                    !Game.SpaceSimulation.TryGetViewObjectComponent<PartOwnerBehavior>(simObject,
                        out var viewObjectComponent))
                {
                    continue;
                }

                foreach (var jointConnection in viewObjectComponent.JointConnections)
                {
                    var item = new JointConnectionItemController { DestroyButtonClicked = OnDestroyButtonClicked };

                    item.Item.RegisterCallback<PointerEnterEvent>(_ => OnJointConnectionItemPointerEntered(ref item));
                    item.Item.RegisterCallback<PointerLeaveEvent>(_ => ClearOutlines());

                    item.SyncTo(jointConnection);
                    _itemsView.Add(item.Item);
                }
            }
        }

        private void RefreshSimObjects(IGGuid activeIGGuid)
        {
            _itemsView.Clear();
            _simObjects.Clear();

            var universeModel = Game?.UniverseModel;
            if (universeModel == null) return;

            foreach (var simObject in universeModel.GetAllSimObjects())
            {
                if (!IsInFilter(simObject, activeIGGuid)) continue;

                var item = new SimObjectItemController { DestroyButtonClicked = OnDestroyButtonClicked };

                item.Item.RegisterCallback<PointerEnterEvent>(_ => OnSimObjectItemPointerEntered(ref item));
                item.Item.RegisterCallback<PointerLeaveEvent>(_ => ClearOutlines());

                item.SyncTo(simObject);
                _itemsView.Add(item.Item);
            }
        }

        private void OnDestroyButtonClicked()
        {
            _forceRefresh = true;
        }

        private bool IsInFilter(SimulationObjectModel? simObject, IGGuid activeVesselGuid)
        {
            if (simObject == null)
            {
                return false;
            }

            var result = false;
            switch (_filterType)
            {
                case ItemType.CelestialBody when simObject?.CelestialBody != null:
                    result = true;
                    break;
                case ItemType.Vessel when simObject?.FindComponent(typeof(IPhysicsOwner)) != null:
                    result = !_showActiveVesselOnly || activeVesselGuid == simObject?.GlobalId;
                    break;
                case ItemType.Part when simObject?.Part != null:
                {
                    result = !_showActiveVesselOnly ||
                             activeVesselGuid == simObject.Part?.PartOwner?.SimulationObject?.GlobalId;
                    break;
                }
                case ItemType.JointConnection:
                default:
                    break;
            }

            return result;
        }

        private void OnSimObjectItemPointerEntered(ref SimObjectItemController item)
        {
            if (!_highlightItems || item.Model == null)
                return;

            switch (_filterType)
            {
                case ItemType.Vessel:
                {
                    if (Game.SpaceSimulation.TryGetViewObjectComponent<VesselBehavior>(item.Model, out var comp))
                        SetVesselHighlight(comp, isHighlighted: true);

                    break;
                }
                case ItemType.Part:
                {
                    if (Game.SpaceSimulation.TryGetViewObjectComponent<PartBehavior>(item.Model, out var comp))
                        SetPartHighlight(comp, isHighlighted: true);

                    break;
                }
                case ItemType.JointConnection:
                case ItemType.CelestialBody:
                default:
                    break;
            }
        }

        private void OnJointConnectionItemPointerEntered(ref JointConnectionItemController item)
        {
            if (_highlightItems && item.Model != null)
            {
                SetJointConnectionHighlight(item.Model, isHighlighted: true);
            }
        }

        private void ClearOutlines()
        {
            Game?.UI?.OutlineManager?.ClearOutlines();
        }

        private void SetVesselHighlight(VesselBehavior vesselBehavior, bool isHighlighted)
        {
            if (vesselBehavior == null || !isHighlighted) return;

            var outlineManager = Game?.UI?.OutlineManager;
            if (outlineManager == null) return;

            outlineManager.ClearOutlines();

            var partOwner = vesselBehavior.PartOwner;
            if (partOwner == null) return;

            foreach (var part in partOwner.Parts)
                outlineManager.AddOutlineToPart(part);
        }

        private void SetPartHighlight(PartBehavior partBehavior, bool isHighlighted)
        {
            if (partBehavior == null || !isHighlighted) return;

            var outlineManager = Game?.UI?.OutlineManager;
            if (outlineManager == null) return;

            outlineManager.ClearOutlines();

            outlineManager.AddOutlineToPart(partBehavior);
        }

        private void SetJointConnectionHighlight(PartOwnerBehavior.JointConnection jointConnection, bool isHighlighted)
        {
            var outlineManager = Game?.UI?.OutlineManager;
            if (!isHighlighted || outlineManager == null) return;

            outlineManager.ClearOutlines();
            
            var host = jointConnection.host;
            var target = jointConnection.target;
            
            if (host != null)
                outlineManager.AddOutlineToPart(host, 0);
            
            if (target != null)
            {
                var colorIndex2 = Mathf.Min(1, outlineManager.lineColors.Length - 1);
                outlineManager.AddOutlineToPart(target, colorIndex2);
            }
        }

        private void OnGameLoadFinished(MessageCenterMessage obj)
        {
            _forceRefresh = true;
        }

        private void OnActiveVesselChanged(MessageCenterMessage obj)
        {
            if (_showActiveVesselOnly)
            {
                _forceRefresh = true;
            }
        }

        private void OnProximityLoadUnloadChanged(ChangeEvent<bool> evt)
        {
            if (Game.UniverseView != null)
                Game.UniverseView.IsProximityLoadUnloadEnabled = evt.newValue;
        }

        private void OnCelestialBodyCollisionApproximationChanged(ChangeEvent<bool> evt)
        {
            if (Game?.SpaceSimulation != null)
                Game.SpaceSimulation.IsCelestialBodyCollisionApproximationEnabled = evt.newValue;
        }

        private void OnHighlightItemsChanged(ChangeEvent<bool> evt)
        {
            _highlightItems = evt.newValue;
        }

        private void OnActivateVesselOnlyChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue != _showActiveVesselOnly)
            {
                _showActiveVesselOnly = evt.newValue;
                _forceRefresh = true;
            }
        }

        private void OnItemTypeChanged(ChangeEvent<string> evt)
        {
            _filterType = (ItemType)Enum.Parse(typeof(ItemType), evt.newValue);
            _forceRefresh = true;
        }
    }
}