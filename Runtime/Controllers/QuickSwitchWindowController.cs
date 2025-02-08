#pragma warning disable CS8618

using KSP.OAB;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class QuickSwitchWindowController : BaseWindowController
    {
        // Buttons
        private Button _shipInVab;
        private Button _shipOnLaunchpad;
        private Button _shipInOrbit;
        private Button _planeInVab;
        private Button _planeOnRunway;
        private Button _vabSurface;
        private Button _vabOrbit;
        private Button _baeSurface;
        private Button _baeOrbit;
        
        private void OnEnable()
        {
            Enable();
            
            _shipInVab = RootElement.Q<Button>("ship-in-vab");
            _shipInVab.clicked += HideOAB;
            _shipInVab.clicked += Game.OAB.VesselInVABNoLaunch;
            
            _shipOnLaunchpad = RootElement.Q<Button>("ship-on-launchpad");
            _shipOnLaunchpad.clicked += () => Game.OAB.VesselOnLaunchpadViaVAB();
            
            _shipInOrbit = RootElement.Q<Button>("ship-in-orbit");
            _shipInOrbit.clicked += () => Game.OAB.VesselInOrbitViaVAB();
            
            _planeInVab = RootElement.Q<Button>("plane-in-vab");
            _planeInVab.clicked += HideOAB;
            _planeInVab.clicked += () => Game.OAB.PlaneInOABNoLaunch();
            
            _planeOnRunway = RootElement.Q<Button>("plane-on-runway");
            _planeOnRunway.clicked += () => Game.OAB.PlaneOnRunwayViaVAB();
            
            _vabSurface = RootElement.Q<Button>("vab-surface");
            _vabSurface.clicked += HideOAB;
            _vabSurface.clicked += () => UserOAB(OABVariant.VAB, OABEnvironmentType.Terrestrial);
            
            _vabOrbit = RootElement.Q<Button>("vab-orbit");
            _vabOrbit.clicked += HideOAB;
            _vabOrbit.clicked += () => UserOAB(OABVariant.VAB, OABEnvironmentType.Orbital);
            
            _baeSurface = RootElement.Q<Button>("bae-surface");
            _baeSurface.clicked += HideOAB;
            _baeSurface.clicked += () => UserOAB(OABVariant.BAE, OABEnvironmentType.Terrestrial);
            
            _baeOrbit = RootElement.Q<Button>("bae-orbit");
            _baeOrbit.clicked += HideOAB;
            _baeOrbit.clicked += () => UserOAB(OABVariant.BAE, OABEnvironmentType.Orbital);
            
        }

        private void HideOAB()
        {
            if (Game?.OAB?.Current?.Stats?.CurrentBuilder?.OABHUD?.header?.OABAutoSave != null)
                Game.OAB.HideOAB();
        }

        private void UserOAB(OABVariant variant, OABEnvironmentType environmentType)
        {
            OABConfig config = new(variant, environmentType, OABConstructionType.User, "Kerbin");
            Game.OAB.ShowOAB(config);
        }
    }
}