using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Game;
using KSP.Rendering;
using KSP.Rendering.Planets;
using KSP.Sim;
using RTG;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class TerrainDebugWindowController : BaseWindowController
    {
        private const string PhysXBubbleMaterialAddress = "Assets/Modules/DebugTools/Assets/UI/PhysXBubbleMat.mat";

        private Toggle _enableMeshBucketing;
        private Toggle _showBiomeColors;
        private Toggle _boostTriplanarContrast;
        private Toggle _renderWireframe;
        private Toggle _includeInterestPosition;
        private Toggle _renderPhysXBubble;

        private DropdownField _showBiomes;
        private DropdownField _showTriplanar;

        private Button _updateTriplanarBasis;
        private Button _resetTriplanarBasis;

        private Label _timeSinceTriplanarBasisUpdate;
        private Label _pqsStats;

        private Button _selectQuad;
        private Label _debugQuadStatus;

        private int _secondsSinceTriplanarBasisUpdated;
        private bool _selectQuadForDebugging;

        private GameObject _flightCamera;

        private Material _physxBubbleMaterial;

        private void OnEnable()
        {
            Enable();

            _enableMeshBucketing = RootElement.Q<Toggle>("enable-mesh-bucketing");
            _enableMeshBucketing.RegisterValueChangedCallback(EnableMeshBucketing);

            _showBiomeColors = RootElement.Q<Toggle>("show-biome-colors");
            _showBiomeColors.RegisterValueChangedCallback(ShowBiomeColors);

            _boostTriplanarContrast = RootElement.Q<Toggle>("boost-triplanar-contrast");
            _boostTriplanarContrast.RegisterValueChangedCallback(BoostTriplanarContrast);

            _renderWireframe = RootElement.Q<Toggle>("render-wireframe");
            _renderWireframe.RegisterValueChangedCallback(RenderWireframe);

            _includeInterestPosition = RootElement.Q<Toggle>("include-interest-position");
            _includeInterestPosition.RegisterValueChangedCallback(IncludeInterestPosition);

            _renderPhysXBubble = RootElement.Q<Toggle>("render-physx-bubble");

            _updateTriplanarBasis = RootElement.Q<Button>("update-triplanar-basis");
            _updateTriplanarBasis.clicked += UpdateTriplanarBasis;

            _resetTriplanarBasis = RootElement.Q<Button>("reset-triplanar-basis");
            _resetTriplanarBasis.clicked += ResetTriplanarBasis;

            _showBiomes = RootElement.Q<DropdownField>("show-biomes");
            _showBiomes.choices = Enum.GetNames(typeof(DebugBiome)).ToList();
            _showBiomes.value = PQSRenderer.DebugBiome.ToString();
            _showBiomes.RegisterValueChangedCallback(ShowBiomeChanged);

            _showTriplanar = RootElement.Q<DropdownField>("show-triplanar");
            _showTriplanar.choices = Enum.GetNames(typeof(DebugTriplanar)).ToList();
            _showTriplanar.value = PQSRenderer.DebugTriplanar.ToString();
            _showTriplanar.RegisterValueChangedCallback(ShowTriplanarChanged);

            _timeSinceTriplanarBasisUpdate = RootElement.Q<Label>("time-since-tp-update");
            _pqsStats = RootElement.Q<Label>("pqs-stats");

            _selectQuad = RootElement.Q<Button>("select-quad");
            _selectQuad.clicked += () => { _selectQuadForDebugging = true; };
            _debugQuadStatus = RootElement.Q<Label>("debug-quad-status");

            GameManager.Instance.Assets.Load<Material>(PhysXBubbleMaterialAddress,
                mat => { _physxBubbleMaterial = mat; });

            EnsureFlightCamera();
        }

        private void EnsureFlightCamera()
        {
            if (_flightCamera == null && Game != null && Game?.CameraManager != null)
            {
                _flightCamera = Game.CameraManager.GetCameraRenderStack(CameraID.Flight, RenderSpaceType.PhysicsSpace)
                    .GetMainRenderCamera().gameObject;
            }
        }

        private static void EnableMeshBucketing(ChangeEvent<bool> evt)
        {
            PQSRenderer.ShaderRefactorState = evt.newValue ? ShaderRefactorState.Standard : ShaderRefactorState.SwapOld;
        }

        private static void ShowBiomeColors(ChangeEvent<bool> evt)
        {
            PQSRenderer.DebugShowBiomeColor = evt.newValue;
        }

        private static void BoostTriplanarContrast(ChangeEvent<bool> evt)
        {
            PQSRenderer.DebugBoostTriplanarContrast = evt.newValue;
        }

        private void RenderWireframe(ChangeEvent<bool> evt)
        {
            EnsureFlightCamera();
            if (_flightCamera == null) return;

            var component = _flightCamera.GetComponent<RenderWireframe>();
            if (evt.newValue && component == null)
                _flightCamera.AddComponent<RenderWireframe>();

            else
                Destroy(component);
        }

        private static void IncludeInterestPosition(ChangeEvent<bool> evt)
        {
            PQS.DebugIncludeInterestPositions = evt.newValue;
        }

        private static void ShowBiomeChanged(ChangeEvent<string> evt)
        {
            PQSRenderer.DebugBiome = (DebugBiome)Enum.Parse(typeof(DebugBiome), evt.newValue);
        }

        private static void ShowTriplanarChanged(ChangeEvent<string> evt)
        {
            PQSRenderer.DebugTriplanar = (DebugTriplanar)Enum.Parse(typeof(DebugTriplanar), evt.newValue);
        }

        private static void UpdateTriplanarBasis()
        {
            PQSRenderer.DebugForceUpdateTriplanarBasis = true;
        }

        private static void ResetTriplanarBasis()
        {
            PQSRenderer.DebugResetTriplanarBasisToDefault = true;
        }

        private void Update()
        {
            if (!IsWindowOpen) return;

            if (_selectQuadForDebugging && Input.GetMouseButtonDown(0))
            {
                var pqsRenderer = FindObjectOfType<PQSRenderer>();
                pqsRenderer?.DebugColliderWithRaycast();
                _selectQuadForDebugging = false;
            }

            _enableMeshBucketing.value = PQSRenderer.ShaderRefactorState == ShaderRefactorState.Standard;
            _showBiomeColors.value = PQSRenderer.DebugShowBiomeColor;
            _boostTriplanarContrast.value = PQSRenderer.DebugBoostTriplanarContrast;

            EnsureFlightCamera();
            if (_flightCamera != null)
            {
                _renderWireframe.SetEnabled(true);
                _renderWireframe.value = _flightCamera.GetComponent<RenderWireframe>() != null;
            }
            else
                _renderWireframe.SetEnabled(false);

            _includeInterestPosition.value = PQS.DebugIncludeInterestPositions;

            var num = (int)Math.Round(Time.timeSinceLevelLoadAsDouble - PQSRenderer.DebugTimeTriplanarBasisUpdated);
            if (num != _secondsSinceTriplanarBasisUpdated)
            {
                _secondsSinceTriplanarBasisUpdated = num;
                _timeSinceTriplanarBasisUpdate.text =
                    $"{_secondsSinceTriplanarBasisUpdated}s since triplanar basis updated.";
            }

            _pqsStats.text = "PQS Stats:" +
                             $" Vessels: {PQS.DebugInterestVesselCount} |" +
                             $" Quads: {PQSRenderer.DebugVisiblePQSQuadCount} |" +
                             $" Colliders: {PQSRenderer.DebugActivePQSColliderCount}";

            _debugQuadStatus.text = _selectQuadForDebugging
                ? "Click on a piece of terrain"
                : $"Quad: {PQSRenderer.DebugQuadIndex}";

            if (_renderPhysXBubble.value)
            {
                var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, -Vector3.one * 2000f);
                Graphics.DrawMesh(Singleton<MeshPool>.Get.UnitSphere, matrix, _physxBubbleMaterial, 0, null, 0, null,
                    castShadows: false, receiveShadows: false, useLightProbes: false);
            }
        }
    }
}