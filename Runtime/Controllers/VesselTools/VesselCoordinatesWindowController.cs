using KSP.Sim.impl;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers.VesselTools
{
    public class VesselCoordinatesWindowController : BaseWindowController
    {
        private const string FP6Places = "F6";

        private Toggle? _toggleHandOfKraken;

        private Label? _latitude;
        private Label? _longitude;
        private Label? _heading;
        private Label? _zenith;
        private Label? _horizonPitch;

        private Label? _celestialBody;
        private Label? _originDistance;
        private Label? _situation;
        private Label? _landed;
        private Label? _splashed;

        private Label? _altitudeRadius;
        private Label? _altitudeTerrain;
        private Label? _altitudeSeaLevel;
        private Label? _altitudeScenery;
        private Label? _altitudeSurface;

        private Label? _semiMajorAxis;
        private Label? _eccentricity;
        private Label? _inclination;
        private Label? _longitudeAscendingNode;
        private Label? _periapsis;
        private Label? _epoch;
        private Label? _meanAnomaly;

        private Label? _hokPhysicsMode;
        private Label? _hokCorrectingOrbit;
        private Label? _hokGotExpectedOrbit;
        private Label? _hokUnderAccel;
        private Label? _hokOrbitStandoff;

        private void OnEnable()
        {
            Enable();

            _toggleHandOfKraken = RootElement.Q<Toggle>("toggle-hok");
            _toggleHandOfKraken.RegisterValueChangedCallback(ToggleHandOfKraken);

            _latitude = RootElement.Q<Label>("latitude");
            _longitude = RootElement.Q<Label>("longitude");
            _heading = RootElement.Q<Label>("heading");
            _zenith = RootElement.Q<Label>("zenith");
            _horizonPitch = RootElement.Q<Label>("horizon-pitch");

            _celestialBody = RootElement.Q<Label>("celestial-body");
            _originDistance = RootElement.Q<Label>("origin-distance");
            _situation = RootElement.Q<Label>("situation");
            _landed = RootElement.Q<Label>("landed");
            _splashed = RootElement.Q<Label>("splashed");

            _altitudeRadius = RootElement.Q<Label>("alt-radius");
            _altitudeTerrain = RootElement.Q<Label>("alt-terrain");
            _altitudeSeaLevel = RootElement.Q<Label>("alt-sl");
            _altitudeScenery = RootElement.Q<Label>("alt-scenery");
            _altitudeSurface = RootElement.Q<Label>("alt-surface");

            _semiMajorAxis = RootElement.Q<Label>("semi-major-axis");
            _eccentricity = RootElement.Q<Label>("eccentricity");
            _inclination = RootElement.Q<Label>("inclination");
            _longitudeAscendingNode = RootElement.Q<Label>("long-asc-node");
            _periapsis = RootElement.Q<Label>("periapsis");
            _epoch = RootElement.Q<Label>("epoch");
            _meanAnomaly = RootElement.Q<Label>("epoch-anomaly");

            _hokPhysicsMode = RootElement.Q<Label>("hok-phys-mode");
            _hokCorrectingOrbit = RootElement.Q<Label>("hok-correcting");
            _hokGotExpectedOrbit = RootElement.Q<Label>("hok-got-pos");
            _hokUnderAccel = RootElement.Q<Label>("hok-under-accel");
            _hokOrbitStandoff = RootElement.Q<Label>("hok-orbit-standoff");
        }

        public void SyncTo(VesselComponent vessel, VesselBehavior behavior)
        {
            _latitude!.text = $"Latitude: <b>{vessel.Latitude.ToString(FP6Places)}</b>";
            _longitude!.text = $"Longitude: <b>{vessel.Longitude.ToString(FP6Places)}</b>";
            _heading!.text = $"Heading: <b>{vessel.Heading.ToString(FP6Places)}</b>";
            _zenith!.text = $"Zenith: <b>{vessel.Zenith.ToString(FP6Places)}</b>";
            _horizonPitch!.text = $"Pitch: <b>{vessel.Pitch_HorizonRelative.ToString(FP6Places)}</b>";

            _celestialBody!.text = $"Body: <b>{vessel.mainBody.bodyName}</b>";
            _originDistance!.text =
                $"Origin: <b>{Game.UniverseView.PhysicsSpace.FloatingOrigin.distanceFromOrigin.ToString(FP6Places)}</b>";
            _situation!.text = $"Situation: <b>{vessel.Situation}</b>";
            _landed!.text = $"Landed: <b>{vessel.Landed}</b>";
            _splashed!.text = $"Landed: <b>{vessel.Splashed}</b>";

            _altitudeRadius!.text = $"Radius: <b>{vessel.AltitudeFromRadius.ToString(FP6Places)}</b>";
            _altitudeTerrain!.text = $"Terrain: <b>{vessel.AltitudeFromTerrain.ToString(FP6Places)}</b>";
            _altitudeSeaLevel!.text = $"Sea Level: <b>{vessel.AltitudeFromSeaLevel.ToString(FP6Places)}</b>";
            _altitudeScenery!.text = $"Scenery: <b>{vessel.AltitudeFromScenery.ToString(FP6Places)}</b>";
            _altitudeSurface!.text = $"Surface: <b>{vessel.AltitudeFromSurface.ToString(FP6Places)}</b>";

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (vessel.Orbiter != null)
            {
                _semiMajorAxis!.text = "Semi Major Axis: <b>" +
                                       $"{vessel.Orbiter.PatchedConicsOrbit.OrbitalElements.SemiMajorAxis.ToString(FP6Places)}</b>";
                _eccentricity!.text = "Eccentricity: <b>" +
                                      $"{vessel.Orbiter.PatchedConicsOrbit.OrbitalElements.Eccentricity.ToString(FP6Places)}</b>";
                _inclination!.text = "Inclination: <b>" +
                                     $"{vessel.Orbiter.PatchedConicsOrbit.OrbitalElements.Inclination.ToString(FP6Places)}</b>";
                _longitudeAscendingNode!.text = "Long. Ascending Node: <b>" +
                                                $"{vessel.Orbiter.PatchedConicsOrbit.OrbitalElements.LongitudeOfAscendingNode.ToString(FP6Places)}</b>";
                _periapsis!.text = "Arg. Periapsis: <b>" +
                                   $"{vessel.Orbiter.PatchedConicsOrbit.OrbitalElements.ArgumentOfPeriapsis.ToString(FP6Places)}</b>";
                _epoch!.text = "Epoch: <b>" +
                               $"{vessel.Orbiter.PatchedConicsOrbit.OrbitalElements.Epoch.ToString(FP6Places)}</b>";
                _meanAnomaly!.text = "Mean Anomaly at Epoch: <b>" +
                                     $"{vessel.Orbiter.PatchedConicsOrbit.OrbitalElements.MeanAnomalyAtEpoch.ToString(FP6Places)}</b>";
            }

            _toggleHandOfKraken!.value = behavior.PartOwner.IsHandOfKrakenEnabled;
            _hokPhysicsMode!.text = $"Physics Mode: <b>{vessel.Physics}</b>";
            _hokCorrectingOrbit!.text = $"Correcting Orbit: <b>{behavior.PartOwner.IsHandOfKrakenTryingToCorrect}</b>";
            _hokGotExpectedOrbit!.text =
                $"Got Expected Orbit: <b>{behavior.PartOwner.HasHandOfKrakenGotExpectedOrbit}</b>";
            _hokUnderAccel!.text =
                $"Under Acceleration: <b>{behavior.PartOwner.IsHandOfKrakenOrbitUnderAcceleration}</b>";
            _hokOrbitStandoff!.text =
                $"Orbit Standoff: <b>{behavior.PartOwner.IsHandOfKrakenOrbitCorrectInStandOff}</b>";
        }

        private void ToggleHandOfKraken(ChangeEvent<bool> evt)
        {
            var activeVessel = Game.ViewController.GetActiveSimVessel();
            var behavior = Game.ViewController.GetBehaviorIfLoaded(activeVessel);
            
            switch (behavior.PartOwner.IsHandOfKrakenEnabled)
            {
                case true when !evt.newValue:
                    behavior.PartOwner.DisableHandOfKraken();
                    break;
                case false when evt.newValue:
                    behavior.PartOwner.EnableHandOfKraken();
                    break;
            }
        }
    }
}