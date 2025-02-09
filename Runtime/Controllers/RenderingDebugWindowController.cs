using System;
using KSP.Messages;
using KSP.Rendering;
using KSP.Sim;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class RenderingDebugWindowController : BaseWindowController
    {
        private Toggle? _physicsCameraToggle;
        private Toggle? _scaledCameraToggle;
        private Toggle? _flareCameraToggle;

        private Camera? PhysicsCamera
        {
            get
            {
                if (Game.CameraManager != null)
                    return Game.CameraManager.GetCameraRenderStack(CameraID.Flight, RenderSpaceType.PhysicsSpace)
                        .GetMainRenderCamera();

                return null;
            }
        }

        private bool PhysicsCameraEnabled => PhysicsCamera?.enabled ?? false;

        private Camera? ScaledCamera
        {
            get
            {
                if (Game.CameraManager != null)
                    return Game.CameraManager.GetCameraRenderStack(CameraID.Flight, RenderSpaceType.ScaleSpace)
                        .GetMainRenderCamera();

                return null;
            }
        }

        private bool ScaledCameraEnabled => ScaledCamera?.enabled ?? false;

        private Camera? FlareCamera => Game.GraphicsManager != null
            ? Game.GraphicsManager.LightingSystem.LensFlareSystem.FlareCamera
            : null;

        private bool FlareCameraEnabled => FlareCamera?.enabled ?? false;

        private void Awake()
        {
            Game.Messages.Subscribe<GameStateChangedMessage>(OnGameStateChanged);
        }

        private void OnEnable()
        {
            Enable();
            
            _physicsCameraToggle = RootElement.Q<Toggle>("physics-camera");
            _physicsCameraToggle.value = PhysicsCameraEnabled;
            _physicsCameraToggle.RegisterValueChangedCallback(OnTogglePhysicsCamera);
            
            _scaledCameraToggle = RootElement.Q<Toggle>("scaled-camera");
            _scaledCameraToggle.value = ScaledCameraEnabled;
            _scaledCameraToggle.RegisterValueChangedCallback(OnToggleScaledCamera);
            
            _flareCameraToggle = RootElement.Q<Toggle>("flare-camera");
            _flareCameraToggle.value = FlareCameraEnabled;
            _flareCameraToggle.RegisterValueChangedCallback(OnToggleFlareCamera);
        }

        private void OnTogglePhysicsCamera(ChangeEvent<bool> evt)
        {
            if (PhysicsCamera == null) return;
            
            PhysicsCamera.enabled = evt.newValue;
        }
        
        private void OnToggleScaledCamera(ChangeEvent<bool> evt)
        {
            if (ScaledCamera == null) return;
            
            ScaledCamera.enabled = evt.newValue;
        }
        
        private void OnToggleFlareCamera(ChangeEvent<bool> evt)
        {
            if (FlareCamera == null) return;
            
            FlareCamera.enabled = evt.newValue;
        }

        private void OnGameStateChanged(MessageCenterMessage msg)
        {
            if (_physicsCameraToggle != null)
                _physicsCameraToggle.value = PhysicsCameraEnabled;
            if (_scaledCameraToggle != null)
                _scaledCameraToggle.value = ScaledCameraEnabled;
            if (_flareCameraToggle != null)
                _flareCameraToggle.value = FlareCameraEnabled;
        }
    }
}