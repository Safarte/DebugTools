using System;
using DebugTools.Runtime.UI.FlightTools;
using KSP.Game;
using KSP.Messages;
using KSP.Sim;
using KSP.Sim.impl;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers.FlightTools
{
    internal class SimObjectItemController : ItemController<SimulationObjectModel, SimObjectItem>
    {
        private readonly string[] _physicsModeEnumNames = Enum.GetNames(typeof(PhysicsMode));
        private readonly string[] _transformFrametypeEnumNames = Enum.GetNames(typeof(TransformFrameType));

        public SimObjectItemController()
        {
            Item.ToggleLoadUnload.clicked += ToggleLoadUnloadViewObject;
            Item.SetActive.clicked += SetActiveViewObject;
            Item.Destroy.clicked += DestroySimObject;
        }

        public override void SyncTo(SimulationObjectModel? simObject)
        {
            if (simObject == null) return;

            Model = simObject;
            Item.TextName.text = simObject.Name;

            var text = "";
            if (simObject.FindComponent(typeof(IPhysicsOwner)) is IPhysicsOwner physicsOwner)
            {
                text = _physicsModeEnumNames[(int)physicsOwner.Physics];
            }

            Item.TextPhysicsMode.text = text;

            var simObjParent = simObject.transform.parent;
            var text2 = simObjParent.transform.Guid + " - " +
                        _transformFrametypeEnumNames[(uint)simObjParent.type];
            Item.TextParentReferenceFrame.text = text2;

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var game = GameManager.Instance.Game;

            var hasPartOwner = Model?.PartOwner != null;
            var isCelestialBody = Model?.CelestialBody != null;
            var isLoaded = IsViewObjectLoaded(game, Model);

            var canToggleLoadUnload = Model != null && (hasPartOwner || isCelestialBody);
            Item.ToggleLoadUnload.text = isLoaded ? "Unload" : "Load";
            Item.ToggleLoadUnload.SetEnabled(canToggleLoadUnload);

            var canDestroy = !isCelestialBody && Model is { IsDestroyedOrBeingDestroyed: false };
            Item.Destroy.SetEnabled(canDestroy);

            var canSetActive = false;
            if (hasPartOwner)
            {
                canSetActive = true;
                if (game.ViewController.TryGetActiveVehicle(out var activeVehicle))
                {
                    canSetActive = Model?.GlobalId != null && activeVehicle.Guid != Model?.GlobalId;
                }
            }

            Item.SetActive.SetEnabled(canSetActive);
        }

        private static bool IsViewObjectLoaded(GameInstance game, SimulationObjectModel? simObject)
        {
            if (game != null && simObject != null)
            {
                return simObject?.CelestialBody != null
                    ? game.UniverseView.AssetLoader.IsScaledSpaceLoaded(simObject.CelestialBody.bodyName) ||
                       game.UniverseView.AssetLoader.IsLoadingScaledSpace(simObject.CelestialBody.bodyName)
                    : game.SpaceSimulation.IsViewObjectLoaded(simObject!);
            }

            return false;
        }

        private void ToggleLoadUnloadViewObject()
        {
            if (Model == null) return;

            if (IsViewObjectLoaded(GameManager.Instance.Game, Model))
            {
                UnloadViewObject();
            }
            else
            {
                LoadViewObject();
            }
        }

        private void UnloadViewObject()
        {
            if (Model == null) return;

            if (Model?.CelestialBody != null)
            {
                UnloadCelestialBodyScaledSpaceView(Model.CelestialBody);
            }
            else
            {
                Model!.DestroyViewObject();
            }

            UpdateButtonStates();
        }

        private void LoadViewObject()
        {
            if (Model == null) return;

            if (Model?.PartOwner != null)
            {
                LoadPartOwnerViewObject(Model.PartOwner);
            }
            else if (Model?.CelestialBody != null)
            {
                LoadCelestialBodyScaledSpaceView(Model.CelestialBody);
            }

            UpdateButtonStates();
        }

        private void LoadPartOwnerViewObject(PartOwnerComponent partOwnerComponent)
        {
            Model?.InstantiateViewObjectAsync();
            foreach (var part in partOwnerComponent.Parts)
            {
                part.SimulationObject.InstantiateViewObjectAsync();
            }
        }

        private static void LoadCelestialBodyScaledSpaceView(CelestialBodyComponent celestialBodyComponent)
        {
            GameManager.Instance.Game.UniverseView.AssetLoader.BeginLoadScaledSpaceView(celestialBodyComponent);
        }

        private static void UnloadCelestialBodyScaledSpaceView(CelestialBodyComponent celestialBodyComponent)
        {
            GameManager.Instance.Game.UniverseView.AssetLoader.UnloadScaledSpaceView(celestialBodyComponent);
        }

        private void SetActiveViewObject()
        {
            if (Model is { CelestialBody: null })
            {
                GameManager.Instance.Game.ViewController.SetActiveVehicle(Model.GlobalId);
            }
        }

        private void DestroySimObject()
        {
            Model?.Destroy();
            DestroyButtonClicked?.Invoke();
        }
    }
}