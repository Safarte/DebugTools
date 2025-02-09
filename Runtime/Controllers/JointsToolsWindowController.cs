using System;
using System.Linq;
using KSP.Game;
using KSP.Modules;
using KSP.Sim;
using KSP.Sim.impl;
using UnityEngine;
using UnityEngine.UIElements;


// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class JointsToolsWindowController : BaseWindowController
    {
        private Label? _vesselName;
        private Label? _physicsMode;
        private Label? _situation;

        private Label? _partsCount;
        private Label? _jointsCount;
        private Label? _physxRBCount;

        private Label? _rigidityValue;
        private Label? _stackRigidityValue;
        private Label? _surfaceRigidityValue;

        private Label? _mode;

        private TextField? _jointRigidityInput;
        private TextField? _stackRigidityInput;
        private TextField? _surfaceRigidityInput;

        private Toggle? _multiJoints;
        private Toggle? _inertiaTensorFix;

        private Toggle? _extraJoints;
        private DropdownField? _extraJointsMode;

        private bool _hasVessel;

        private VesselComponent? _vessel;
        private VesselBehavior? _vesselBehavior;
        private PartOwnerComponent? _partOwner;

        private int _jointConnections;
        private int _virtualJointConnections;
        private int _physxJoints;

        private readonly float _jointCountSeconds = 2f;
        private float _jointsLastCounted = -1f;

        private void OnEnable()
        {
            Enable();

            _vesselName = RootElement.Q<Label>("vessel-name");
            _physicsMode = RootElement.Q<Label>("physics-mode");
            _situation = RootElement.Q<Label>("situation");

            _partsCount = RootElement.Q<Label>("parts-count");
            _jointsCount = RootElement.Q<Label>("joints-count");
            _physxRBCount = RootElement.Q<Label>("physx-rb-count");

            _rigidityValue = RootElement.Q<Label>("rigidity-value");
            _stackRigidityValue = RootElement.Q<Label>("stack-rigidity-value");
            _surfaceRigidityValue = RootElement.Q<Label>("surface-rigidity-value");

            _mode = RootElement.Q<Label>("mode-value");
            var setPacked = RootElement.Q<Button>("set-packed");
            setPacked.clicked += SetPacked;
            var setUnpacked = RootElement.Q<Button>("set-unpacked");
            setUnpacked.clicked += SetUnpacked;

            _jointRigidityInput = RootElement.Q<TextField>("joint-rigidity-input");
            var setJointRigidity = RootElement.Q<Button>("set-joint-rigidity");
            setJointRigidity.clicked += SetJointRigidity;

            _stackRigidityInput = RootElement.Q<TextField>("stack-rigidity-input");
            var setStackRigidity = RootElement.Q<Button>("set-stack-rigidity");
            setStackRigidity.clicked += SetStackRigidity;

            _surfaceRigidityInput = RootElement.Q<TextField>("surface-rigidity-input");
            var setSurfaceRigidity = RootElement.Q<Button>("set-surface-rigidity");
            setSurfaceRigidity.clicked += SetSurfaceRigidity;

            _multiJoints = RootElement.Q<Toggle>("multi-joints");
            _multiJoints.RegisterValueChangedCallback(MultiJointsChanged);

            _inertiaTensorFix = RootElement.Q<Toggle>("inertia-tensor-fix");
            _inertiaTensorFix.RegisterValueChangedCallback(InertiaTensorFixChanged);

            _extraJoints = RootElement.Q<Toggle>("extra-joints");
            _extraJoints.RegisterValueChangedCallback(ExtraJointsChanged);

            _extraJointsMode = RootElement.Q<DropdownField>("extra-joints-mode");
            _extraJointsMode.choices = Enum.GetNames(typeof(Data_ReinforcedConnection.ConnectionType)).ToList();
            _extraJointsMode.value = PersistentProfileManager.EnhancedJointsMode.ToString();
            _extraJointsMode.RegisterValueChangedCallback(ExtraJointsModeChanged);
        }

        private void LateUpdate()
        {
            if (!IsWindowOpen) return;
            
            _rigidityValue!.text = $"Rigidity: <b>{PhysicsSettings.JOINT_RIGIDITY:0.0}</b>";
            _stackRigidityValue!.text = $"Stack Rigidity: <b>{PhysicsSettings.JOINT_STACK_NODE_FACTOR:0.0}</b>";
            _surfaceRigidityValue!.text = $"Surface Rigidity: <b>{PhysicsSettings.JOINT_SURFACE_NODE_FACTOR:0.0}</b>";
            
            _hasVessel = false;
            if (Game?.ViewController != null)
                _hasVessel = Game.ViewController.TryGetActiveSimVessel(out _vessel);

            if (!_hasVessel)
            {
                _vesselName!.text = "None";
                _physicsMode!.text = "Physics Mode: ";
                _situation!.text = "Situation: ";
                _partsCount!.text = "Parts: ";
                _jointsCount!.text = "Joints: ";
                _physxRBCount!.text = "RigidBodies: ";
                _mode!.text = "RigidBody Mode: ";
                return;
            }

            _vesselBehavior = Game?.ViewController.GetBehaviorIfLoaded(_vessel!);
            _partOwner = _vessel?.SimulationObject.PartOwner;
            
            _vesselName!.text = $"<b>{_vessel?.DisplayName}</b>";
            _physicsMode!.text = $"PhysicsMode: <b>{_vessel?.Physics.ToString()}</b>";
            _situation!.text = $"Situation: <b>{_vessel?.Situation.ToString()}</b>";
            
            _partsCount!.text = $"Parts: <b>{_partOwner?.PartCount.ToString()}</b>";
            
            _jointsLastCounted -= Time.unscaledDeltaTime;
            if (_jointsLastCounted >= 0f) return;
            
            _jointsLastCounted = _jointCountSeconds;
            
            var partJoints = 0;
            foreach (var jointConnection in _vesselBehavior!.PartOwner.JointConnections)
                if (jointConnection?.Joints != null)
                    partJoints += jointConnection.Joints.Count();
                
            var reinforcedJoints = 0;
            foreach (var part in _vesselBehavior.PartOwner.Parts)
                if (part.Modules.TryGetValue(typeof(Module_ReinforcedConnection), out var val))
                    reinforcedJoints += ((Module_ReinforcedConnection)val).ReinforcedConfigurableJoints;
                
            _jointsCount!.text = $"Joints: <b>{partJoints:0}</b> part - <b>{reinforcedJoints:0}</b> reinf.";
            
            var physxRBCount = 0;
            foreach (var part2 in _partOwner!.Parts)
                if (part2.PhysicsMode != PartPhysicsModes.None)
                    physxRBCount++;
            
            _physxRBCount!.text = $"RigidBodies: <b>{physxRBCount:0}</b>";
            
            var rigidBodyMode = "RigidBody Mode: ";
            if (_vessel?.Physics == PhysicsMode.RigidBody && _vesselBehavior != null)
                rigidBodyMode += _vesselBehavior.IsUnpacked() ? "<b>Unpacked</b>" : "<b>Packed</b>";
            
            _mode!.text = rigidBodyMode;

            _multiJoints!.value = PersistentProfileManager.MultiJointsEnabled;
            _inertiaTensorFix!.value = PhysicsSettings.ENABLE_INERTIA_TENSOR_SCALING;

            _extraJoints!.value = PersistentProfileManager.EnhancedJointsEnabled;
            _extraJointsMode!.value = PersistentProfileManager.EnhancedJointsMode.ToString();
        }

        private void SetPacked()
        {
            if (_hasVessel)
                _vesselBehavior?.DebugForcePackVessel();
        }

        private void SetUnpacked()
        {
            if (_hasVessel)
                _vesselBehavior?.DebugForceUnpackVessel();
        }

        private void SetJointRigidity()
        {
            if (float.TryParse(_jointRigidityInput?.text, out var result))
                PhysicsSettings.JOINT_RIGIDITY = result;
        }

        private void SetStackRigidity()
        {
            if (float.TryParse(_stackRigidityInput?.text, out var result))
                PhysicsSettings.JOINT_STACK_NODE_FACTOR = result;
        }

        private void SetSurfaceRigidity()
        {
            if (float.TryParse(_surfaceRigidityInput?.text, out var result))
                PhysicsSettings.JOINT_SURFACE_NODE_FACTOR = result;
        }

        private static void ExtraJointsChanged(ChangeEvent<bool> evt)
        {
            PersistentProfileManager.EnhancedJointsEnabled = evt.newValue;
        }

        private static void ExtraJointsModeChanged(ChangeEvent<string> evt)
        {
            PersistentProfileManager.EnhancedJointsMode =
                (Data_ReinforcedConnection.ConnectionType)Enum.Parse(typeof(Data_ReinforcedConnection.ConnectionType),
                    evt.newValue);
        }

        private static void MultiJointsChanged(ChangeEvent<bool> evt)
        {
            PersistentProfileManager.MultiJointsEnabled = evt.newValue;
        }

        private static void InertiaTensorFixChanged(ChangeEvent<bool> evt)
        {
            PhysicsSettings.ENABLE_INERTIA_TENSOR_SCALING = evt.newValue;
        }
    }
}