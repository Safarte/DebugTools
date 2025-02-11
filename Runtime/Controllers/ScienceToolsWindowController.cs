using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers
{
    public class ScienceToolsWindowController : BaseWindowController
    {
        private Label? _agencyName;
        private Label? _scienceState;

        private Toggle? _unlockAllParts;

        private TextField? _addSciencePointsField;
        private TextField? _setPointsCapacityField;
        private TextField? _setAllocatedPointsField;

        private DropdownField? _availableNodes;

        private ScrollView? _loadedNodes;
        private ScrollView? _unlockedNodes;

        private const float UpdateSeconds = 2f;
        private float _lastUpdate = -1f;

        private void OnEnable()
        {
            Enable();

            _agencyName = RootElement.Q<Label>("agency-name");
            _scienceState = RootElement.Q<Label>("science-state");

            _unlockAllParts = RootElement.Q<Toggle>("unlock-all-parts");
            _unlockAllParts.value = Game.CheatSystem.Get(CheatSystemItemID.UnlockAllParts);
            _unlockAllParts.RegisterValueChangedCallback(UnlockAllPartsChanged);

            _addSciencePointsField = RootElement.Q<TextField>("add-points-field");
            var addPoints = RootElement.Q<Button>("add-points");
            addPoints.clicked += AddSciencePoints;

            _setPointsCapacityField = RootElement.Q<TextField>("set-capacity-field");
            var setCapacity = RootElement.Q<Button>("set-capacity");
            setCapacity.clicked += SetPointsCapacity;

            _setAllocatedPointsField = RootElement.Q<TextField>("set-allocated-field");
            var setAllocated = RootElement.Q<Button>("set-allocated");
            setAllocated.clicked += SetAllocatedPoints;

            _availableNodes = RootElement.Q<DropdownField>("available-nodes");
            var unlockNode = RootElement.Q<Button>("unlock-node");
            unlockNode.clicked += UpdateUnlockedTechNodes;

            _loadedNodes = RootElement.Q<ScrollView>("loaded-nodes");
            _unlockedNodes = RootElement.Q<ScrollView>("unlocked-nodes");
        }

        private void LateUpdate()
        {
            if (!IsWindowOpen) return;

            if (_unlockAllParts != null)
                _unlockAllParts.value = Game.CheatSystem.Get(CheatSystemItemID.UnlockAllParts);
            
            _lastUpdate -= Time.unscaledDeltaTime;
            if (_lastUpdate >= 0f) return;
            _lastUpdate = UpdateSeconds;
            
            UpdateScienceMetadata();
            UpdateTechNodeDropdown();
            UpdateLoadedTechNodesView();
            UpdateUnlockedTechNodesView();
        }

        private void UnlockAllPartsChanged(ChangeEvent<bool> evt)
        {
            Game.CheatSystem.Set(CheatSystemItemID.UnlockAllParts, evt.newValue);
            UpdateUnlockedTechNodesView();
        }

        private void AddSciencePoints()
        {
            if (int.TryParse(_addSciencePointsField?.text, out var result))
            {
                var additionalSciencePoints = Game.SessionManager.GetMyAgencyAdditionalSciencePoints() + result;
                Game.SessionManager.UpdateMyAgencyAdditionalSciencePoints(additionalSciencePoints);
                UpdateScienceMetadata();
            }
        }

        private void SetPointsCapacity()
        {
            if (int.TryParse(_setPointsCapacityField?.text, out var result))
            {
                Game.SessionManager.UpdateMyAgencySciencePointCapacity(result);
                UpdateScienceMetadata();
            }
        }

        private void SetAllocatedPoints()
        {
            if (int.TryParse(_setAllocatedPointsField?.text, out var result))
            {
                Game.SessionManager.SetMyPlayerAllocatedSciencePoints(result);
                UpdateScienceMetadata();
            }
        }

        private void UpdateScienceMetadata()
        {
            var sessionManager = Game.SessionManager;

            _agencyName!.text = "Agency Name: <b>" + sessionManager.GetMyAgencyName() + "</b>";

            sessionManager.TryGetDifficultyOptionState<float>("StartingScience", out var startingScience);
            var capacity = (float)sessionManager.GetMyAgencySciencePointCapacity();
            var allocated = (float)sessionManager.GetMyPlayerAllocatedSciencePoints();
            var additional = (float)sessionManager.GetMyAgencyAdditionalSciencePoints();
            var total = capacity + startingScience + additional;

            var text = "Available: <b>" + (total - allocated) + "</b> - Used: <b>" + allocated +
                       "</b> - Total: <b>" + total + "</b>";
            _scienceState!.text = text;
        }

        private void UpdateTechNodeDropdown()
        {
            if (_availableNodes == null) return;

            List<string> list = new();
            if (Game.CampaignPlayerManager.TryGetMyCampaignPlayerEntry(out var campaignPlayerEntryOut))
            {
                foreach (var value in Game.ScienceManager.TechNodeDataStore.AvailableData.Values)
                {
                    if (campaignPlayerEntryOut.UnlockedTechNodes.Contains(value.ID))
                        continue;

                    var allRequiredUnlocked = true;
                    foreach (var nodeIds in value.RequiredTechNodeIDs)
                    {
                        if (!Game.ScienceManager.IsNodeUnlocked(nodeIds))
                        {
                            allRequiredUnlocked = false;
                            break;
                        }
                    }

                    if (allRequiredUnlocked)
                        list.Add("<b>" + value.ID + "</b>");
                    else
                        list.Add(value.ID);
                }
            }

            list.Sort();
            _availableNodes.choices = list;
        }

        private void UpdateUnlockedTechNodes()
        {
            if (_availableNodes?.choices.Count <= 0) return;
            
            var node = _availableNodes!.value;
            if (!string.IsNullOrWhiteSpace(node) &&
                Game.CampaignPlayerManager.TryGetMyCampaignPlayerEntry(out var player))
            {
                player.AddUnlockedTechNodeToPlayer(node.Replace("<b>", "").Replace("</b>", ""));
                Game.ScienceManager.UpdateAllocatedSciencePoints();
            }

            UpdateTechNodeDropdown();
            UpdateUnlockedTechNodesView();
            UpdateScienceMetadata();
        }

        private void UpdateLoadedTechNodesView()
        {
            _loadedNodes!.Clear();
            foreach (var nodeData in Game.ScienceManager.TechNodeDataStore.AvailableData.Values)
            {
                var label = new Label { text = nodeData.ID };
                _loadedNodes!.Add(label);
            }
        }

        private void UpdateUnlockedTechNodesView()
        {
            _unlockedNodes!.Clear();
            if (!Game.CampaignPlayerManager.TryGetMyCampaignPlayerEntry(out var campaignPlayerEntry)) return;

            foreach (var unlockedTechNode in campaignPlayerEntry.UnlockedTechNodes)
            {
                var label = new Label { text = unlockedTechNode };
                _unlockedNodes!.Add(label);
            }
        }
    }
}