using System.Collections.Generic;
using System.Linq;
using KSP.Game.Science;
using KSP.Messages;
using KSP.Sim.impl;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class VesselScienceWindowController : BaseWindowController
    {
        private Label? _scienceSituation;
        private Label? _scienceRegion;
        private Label? _scienceScalars;

        private DropdownField? _vessel;
        private Toggle? _useActive;

        private readonly List<VesselComponent> _allVessels = new();

        private void OnEnable()
        {
            Enable();

            _scienceSituation = RootElement.Q<Label>("situation");
            _scienceRegion = RootElement.Q<Label>("region");
            _scienceScalars = RootElement.Q<Label>("scalars");

            _vessel = RootElement.Q<DropdownField>("vessel");
            _useActive = RootElement.Q<Toggle>("use-active");
            _useActive.value = true;
        }

        private void Awake()
        {
            Game.Messages.Subscribe<AddVesselToMapMessage>(OnVesselAdded);
            Game.Messages.Subscribe<VesselDestroyedMessage>(OnVesselRemoved);
            Game.Messages.Subscribe<GameLoadFinishedMessage>(OnGameLoaded);
        }

        private void OnDestroy()
        {
            Game.Messages.Unsubscribe<AddVesselToMapMessage>(OnVesselAdded);
            Game.Messages.Unsubscribe<VesselDestroyedMessage>(OnVesselRemoved);
            Game.Messages.Unsubscribe<GameLoadFinishedMessage>(OnGameLoaded);
        }

        private void OnGameLoaded(MessageCenterMessage msg)
        {
            _allVessels.AddRange(Game.UniverseModel.GetAllVessels());
            UpdateVesselDropdown();
        }

        private VesselComponent? GetChosenVessel()
        {
            return _vessel!.index >= 0 && _vessel!.index < _allVessels.Count ? _allVessels[_vessel.index] : null;
        }

        private void UpdateVesselDropdown()
        {
            _vessel!.choices.Clear();
            _vessel.choices = _allVessels.Select(p => p.DisplayName + " (" + p.mainBody.bodyName + ")").ToList();
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

        private void LateUpdate()
        {
            if (!IsWindowOpen) return;

            _vessel?.SetEnabled(!_useActive!.value);
            VesselComponent? vessel;
            if (_useActive!.value)
            {
                vessel = Game.ViewController.GetActiveSimVessel();
                var value = _allVessels.IndexOf(vessel);
                _vessel!.index = value;
            }
            else
                vessel = GetChosenVessel();

            if (vessel == null) return;

            var situation = vessel.VesselScienceRegionSituation;

            _scienceSituation!.text =
                $"Situation: <b>{situation.ResearchLocation.ScienceSituation.GetTranslatedDescription()}</b>" +
                $" (<b>{situation.ResearchLocation.ScienceSituation}</b>)";

            _scienceRegion!.text =
                $"Region: <b>{ScienceRegionsHelper.GetRegionDisplayName(situation.ResearchLocation.ScienceRegion)}</b>" +
                $" (<b>{situation.ResearchLocation.ScienceRegion}</b>)";

            _scienceScalars!.text = $"Scalars (CB/Sit./Reg.): " +
                                    $"{situation.CelestialBodyScalar}/{situation.SituationScalar}/{situation.ScienceRegionScalar}";
        }
    }
}