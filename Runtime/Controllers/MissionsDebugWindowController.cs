using System.Collections.Generic;
using System.Linq;
using DebugTools.Runtime.UI;
using KSP.Game.Missions;
using KSP.Game.Missions.Definitions;
using KSP.Game.Missions.State;
using KSP.Messages;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers
{
    public class MissionsDebugWindowController : BaseWindowController
    {
        private const string FilterAll = "All";
        private const string FilterActive = "Active Only";
        private const string FilterCompleted = "Completed";
        private const string FilterPrimary = "Primary";
        private const string FilterSecondary = "Secondary";
        private const string FilterTutorial = "Tutorial";
        private const string FilterFTUE = "FTUE";

        private static readonly List<string> FilterChoices = new()
        {
            FilterAll, FilterActive, FilterCompleted,
            FilterPrimary, FilterSecondary, FilterTutorial, FilterFTUE
        };

        private DropdownField _typeFilter;
        private TextField _search;
        private Button _refresh;
        private ScrollView _missionsList;
        private VisualElement _details;

        private Label _headerId;
        private Label _headerName;
        private Label _headerType;
        private Label _headerOwner;
        private Label _headerState;

        private Button _btnActivate;
        private Button _btnDeactivate;
        private Button _btnComplete;
        private Button _btnFail;
        private Button _btnReset;
        private Button _btnRemove;

        private Foldout _statusDump;
        private TextField _statusText;
        private ScrollView _stagesList;

        private readonly List<MissionRow> _rows = new();
        private string _selectedMissionId;
        private bool _selectedIsActive;

        private bool _dirty = true;
        private float _nextRefreshTime;
        private const float RefreshThrottle = 0.5f;

        private void Awake()
        {
            Game.Messages.Subscribe<NewMissionAvailableMessage>(OnMissionMessage);
            Game.Messages.Subscribe<MissionCompleteMessage>(OnMissionMessage);
            Game.Messages.Subscribe<MissionStageCompleted>(OnMissionMessage);
            Game.Messages.Subscribe<OnMissionStageCompleted>(OnMissionMessage);
            Game.Messages.Subscribe<GameLoadFinishedMessage>(OnMissionMessage);
            Game.Messages.Subscribe<GameStateEnteredMessage>(OnMissionMessage);
        }

        private void OnDestroy()
        {
            Game.Messages.Unsubscribe<NewMissionAvailableMessage>(OnMissionMessage);
            Game.Messages.Unsubscribe<MissionCompleteMessage>(OnMissionMessage);
            Game.Messages.Unsubscribe<MissionStageCompleted>(OnMissionMessage);
            Game.Messages.Unsubscribe<OnMissionStageCompleted>(OnMissionMessage);
            Game.Messages.Unsubscribe<GameLoadFinishedMessage>(OnMissionMessage);
            Game.Messages.Unsubscribe<GameStateEnteredMessage>(OnMissionMessage);
        }

        private void OnEnable()
        {
            Enable();

            _typeFilter = RootElement.Q<DropdownField>("type-filter");
            _typeFilter.choices = FilterChoices;
            _typeFilter.value = FilterAll;
            _typeFilter.RegisterValueChangedCallback(_ => _dirty = true);

            _search = RootElement.Q<TextField>("search");
            _search.RegisterValueChangedCallback(_ => _dirty = true);

            _refresh = RootElement.Q<Button>("refresh");
            _refresh.clicked += () => _dirty = true;

            _missionsList = RootElement.Q<ScrollView>("missions-list");
            _missionsList.Clear();

            _details = RootElement.Q<VisualElement>("details");

            _headerId = RootElement.Q<Label>("header-id");
            _headerName = RootElement.Q<Label>("header-name");
            _headerType = RootElement.Q<Label>("header-type");
            _headerOwner = RootElement.Q<Label>("header-owner");
            _headerState = RootElement.Q<Label>("header-state");

            _btnActivate = RootElement.Q<Button>("btn-activate");
            _btnDeactivate = RootElement.Q<Button>("btn-deactivate");
            _btnComplete = RootElement.Q<Button>("btn-complete");
            _btnFail = RootElement.Q<Button>("btn-fail");
            _btnReset = RootElement.Q<Button>("btn-reset");
            _btnRemove = RootElement.Q<Button>("btn-remove");

            _btnActivate.clicked += OnActivateClicked;
            _btnDeactivate.clicked += () => SetSelectedState(MissionState.Inactive);
            _btnComplete.clicked += () => SetSelectedState(MissionState.Complete);
            _btnFail.clicked += () => SetSelectedState(MissionState.Failed);
            _btnReset.clicked += OnResetClicked;
            _btnRemove.clicked += OnRemoveClicked;

            _statusDump = RootElement.Q<Foldout>("status-dump");
            _statusText = RootElement.Q<TextField>("status-text");
            _stagesList = RootElement.Q<ScrollView>("stages-list");

            ClearDetails();
        }

        private void OnMissionMessage(MessageCenterMessage _)
        {
            _dirty = true;
        }

        private void LateUpdate()
        {
            if (!IsWindowOpen) return;
            if (UnityEngine.Time.unscaledTime < _nextRefreshTime && !_dirty) return;
            _nextRefreshTime = UnityEngine.Time.unscaledTime + RefreshThrottle;

            if (_dirty)
            {
                RebuildMissionList();
                _dirty = false;
            }

            if (!string.IsNullOrEmpty(_selectedMissionId))
                RefreshSelectedDetails();
        }

        private KSP2MissionManager Manager => Game?.KSP2MissionManager;

        private IEnumerable<(MissionData data, bool isActive)> EnumerateMissions()
        {
            var manager = Manager;
            if (manager == null) yield break;

            var activeIds = new HashSet<string>();
            foreach (var active in manager.ActiveMissions)
            {
                if (active?.MissionDatas == null) continue;
                foreach (var md in active.MissionDatas)
                {
                    if (md == null) continue;
                    activeIds.Add(md.ID);
                    yield return (md, true);
                }
            }

            foreach (var def in manager.GetMissionDefinitions())
            {
                if (def == null) continue;
                if (activeIds.Contains(def.ID)) continue;
                yield return (def, false);
            }
        }

        private bool PassesFilter(MissionData data, bool isActive)
        {
            var filter = _typeFilter?.value ?? FilterAll;
            switch (filter)
            {
                case FilterActive:
                    if (!isActive) return false;
                    break;
                case FilterCompleted:
                    if (Manager == null || !Manager.CompletedMissionIDs.Contains(data.ID)) return false;
                    break;
                case FilterPrimary:
                    if (data.type != MissionType.Primary) return false;
                    break;
                case FilterSecondary:
                    if (data.type != MissionType.Secondary) return false;
                    break;
                case FilterTutorial:
                    if (data.type != MissionType.Tutorial) return false;
                    break;
                case FilterFTUE:
                    if (data.type != MissionType.FTUE) return false;
                    break;
            }

            var query = _search?.value;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                if ((data.ID == null || data.ID.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) < 0) &&
                    (data.name == null || data.name.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) < 0))
                {
                    return false;
                }
            }

            return true;
        }

        private void RebuildMissionList()
        {
            if (_missionsList == null) return;

            _missionsList.Clear();
            _rows.Clear();

            var entries = EnumerateMissions()
                .Where(e => PassesFilter(e.data, e.isActive))
                .OrderBy(e => e.data.type)
                .ThenBy(e => e.data.ID);

            var foundSelected = false;
            foreach (var (data, isActive) in entries)
            {
                var row = new MissionRow { IsHeader = false };
                BindRow(row, data, isActive);

                var capturedId = data.ID;
                var capturedActive = isActive;
                row.OnSelect = () => SelectMission(capturedId, capturedActive);

                if (capturedId == _selectedMissionId)
                {
                    row.SetSelected(true);
                    foundSelected = true;
                }

                _rows.Add(row);
                _missionsList.Add(row);
            }

            if (!foundSelected)
            {
                _selectedMissionId = null;
                ClearDetails();
            }
        }

        private static void BindRow(MissionRow row, MissionData data, bool isActive)
        {
            row.Id.text = data.ID;
            row.Name.text = string.IsNullOrEmpty(data.name) ? "-" : data.name;
            row.Type.text = data.type.ToString();
            row.Owner.text = $"{data.Owner}";
            var stateText = isActive ? data.state.ToString() : "Inactive";
            row.State.text = stateText;
            row.StageInfo.text = data.missionStages == null || data.missionStages.Count == 0
                ? "-"
                : $"{data.currentStageIndex + 1}/{data.missionStages.Count}";
            row.SetStateClass(stateText);
        }

        private void SelectMission(string id, bool isActive)
        {
            _selectedMissionId = id;
            _selectedIsActive = isActive;

            foreach (var row in _rows)
                row.SetSelected(row.Id.text == id);

            RefreshSelectedDetails();
        }

        private bool TryGetSelected(out MissionData data, out bool isActive)
        {
            data = null;
            isActive = false;
            if (string.IsNullOrEmpty(_selectedMissionId) || Manager == null) return false;

            foreach (var am in Manager.ActiveMissions)
            {
                if (am?.MissionDatas == null) continue;
                foreach (var md in am.MissionDatas)
                {
                    if (md?.ID == _selectedMissionId)
                    {
                        data = md;
                        isActive = true;
                        return true;
                    }
                }
            }

            foreach (var def in Manager.GetMissionDefinitions())
            {
                if (def?.ID == _selectedMissionId)
                {
                    data = def;
                    isActive = false;
                    return true;
                }
            }

            return false;
        }

        private void RefreshSelectedDetails()
        {
            if (!TryGetSelected(out var data, out var isActive))
            {
                ClearDetails();
                return;
            }

            _selectedIsActive = isActive;

            _headerId.text = $"ID: {data.ID}";
            _headerName.text = $"Name: {(string.IsNullOrEmpty(data.name) ? "-" : data.name)}";
            _headerType.text = $"Type: {data.type}";
            _headerOwner.text = $"Owner: {data.Owner}";
            _headerState.text = $"State: {(isActive ? data.state.ToString() : "Not Active")}";

            _btnActivate.SetEnabled(!isActive || data.state != MissionState.Active);
            _btnDeactivate.SetEnabled(isActive && data.state == MissionState.Active);
            _btnComplete.SetEnabled(isActive && data.state != MissionState.Complete);
            _btnFail.SetEnabled(isActive && data.state != MissionState.Failed);
            _btnReset.SetEnabled(isActive);
            _btnRemove.SetEnabled(isActive);

            if (_statusDump?.value == true)
                _statusText.value = data.GenerateMissionStatus();

            BuildStagesList(data, isActive);
        }

        private void BuildStagesList(MissionData data, bool isActive)
        {
            _stagesList.Clear();
            if (data.missionStages == null) return;

            for (var i = 0; i < data.missionStages.Count; i++)
            {
                var stage = data.missionStages[i];
                var isCurrent = isActive && i == data.currentStageIndex && data.state == MissionState.Active;
                _stagesList.Add(BuildStageRow(data, i, stage, isCurrent, isActive));
            }
        }

        private VisualElement BuildStageRow(MissionData data, int index, MissionStage stage, bool isCurrent, bool isActive)
        {
            var row = new VisualElement();
            row.AddToClassList("stage-row");
            if (isCurrent) row.AddToClassList("stage-row__current");

            if (stage.completed) row.AddToClassList("stage-row__completed");
            else if (stage.active) row.AddToClassList("stage-row__active");
            else row.AddToClassList("stage-row__inactive");

            var indexLabel = new Label($"#{index}");
            indexLabel.AddToClassList("stage-row__index");
            row.Add(indexLabel);

            var nameLabel = new Label(string.IsNullOrEmpty(stage.name) ? $"Stage {stage.StageID}" : stage.name);
            nameLabel.AddToClassList("stage-row__name");
            row.Add(nameLabel);

            var objLabel = new Label(string.IsNullOrEmpty(stage.Objective) ? "-" : stage.Objective);
            objLabel.AddToClassList("stage-row__objective");
            objLabel.tooltip = stage.Objective;
            row.Add(objLabel);

            string statusText;
            if (stage.completed) statusText = "Completed";
            else if (stage.active) statusText = "Active";
            else statusText = "Inactive";
            var statusLabel = new Label(statusText);
            statusLabel.AddToClassList("stage-row__status");
            row.Add(statusLabel);

            var activateBtn = new Button { text = "Jump" };
            activateBtn.AddToClassList("stage-row__activate");
            activateBtn.tooltip = "Jump mission to this stage";
            activateBtn.SetEnabled(isActive);
            activateBtn.clicked += () => OnJumpToStage(data, index);
            row.Add(activateBtn);

            var completeBtn = new Button { text = "Complete" };
            completeBtn.AddToClassList("stage-row__complete");
            completeBtn.tooltip = isCurrent
                ? "Complete this stage (fires events, advances mission)"
                : "Flag this stage's completed=true (does not fire events)";
            completeBtn.SetEnabled(isActive && !stage.completed);
            completeBtn.clicked += () => OnCompleteStage(data, index, isCurrent);
            row.Add(completeBtn);

            return row;
        }

        private void ClearDetails()
        {
            _headerId.text = "ID: -";
            _headerName.text = "Name: -";
            _headerType.text = "Type: -";
            _headerOwner.text = "Owner: -";
            _headerState.text = "State: -";
            _btnActivate.SetEnabled(false);
            _btnDeactivate.SetEnabled(false);
            _btnComplete.SetEnabled(false);
            _btnFail.SetEnabled(false);
            _btnReset.SetEnabled(false);
            _btnRemove.SetEnabled(false);
            _statusText.value = "";
            _stagesList.Clear();
        }

        private void OnActivateClicked()
        {
            if (Manager == null || string.IsNullOrEmpty(_selectedMissionId)) return;

            if (!TryGetSelected(out var data, out var isActive))
            {
                Manager.AddNewActiveMission(_selectedMissionId);
                _dirty = true;
                return;
            }

            if (!isActive)
            {
                Manager.AddNewActiveMission(_selectedMissionId);
            }
            else if (data.state != MissionState.Active)
            {
                Manager.SetMissionState(_selectedMissionId, MissionState.Active);
            }

            _dirty = true;
        }

        private void SetSelectedState(MissionState state)
        {
            if (Manager == null || string.IsNullOrEmpty(_selectedMissionId)) return;
            if (!TryGetSelected(out _, out var isActive) || !isActive) return;

            Manager.SetMissionState(_selectedMissionId, state);
            _dirty = true;
        }

        private void OnResetClicked()
        {
            if (Manager == null || !TryGetSelected(out var data, out var isActive) || !isActive) return;
            Manager.ResetMissionState(data.Owner, data.GetOwnerId(), data.ID);
            _dirty = true;
        }

        private void OnRemoveClicked()
        {
            if (Manager == null || !TryGetSelected(out var data, out var isActive) || !isActive) return;
            Manager.RemoveActiveMission(data);
            _dirty = true;
        }

        private void OnJumpToStage(MissionData data, int targetIndex)
        {
            if (data?.missionStages == null || targetIndex < 0 || targetIndex >= data.missionStages.Count) return;

            var current = data.currentStageIndex;
            if (current >= 0 && current < data.missionStages.Count && data.missionStages[current].active)
                data.missionStages[current].Deactivate();

            data.currentStageIndex = targetIndex;
            data.ReactivateCurrentStage();
            _dirty = true;
        }

        private void OnCompleteStage(MissionData data, int index, bool isCurrent)
        {
            if (data?.missionStages == null || index < 0 || index >= data.missionStages.Count) return;

            if (isCurrent)
            {
                data.OnStageComplete();
            }
            else
            {
                data.missionStages[index].completed = true;
            }

            _dirty = true;
        }
    }
}