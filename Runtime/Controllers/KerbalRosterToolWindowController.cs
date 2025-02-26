using System;
using System.Collections;
using DebugTools.Runtime.UI;
using KSP.Game;
using KSP.Messages;
using UnityEngine;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers
{
    public class KerbalRosterToolWindowController : BaseWindowController
    {
        private Button? _addRandomKerbal;
        private Button? _resetKerbalRoster;

        private TextField? _addByNameField;
        private Button? _addByName;

        private TextField? _addXKerbalsField;
        private Button? _addXKerbals;

        private ScrollView? _kerbals;

        private KerbalRosterManager? _roster;
        
        private bool _rosterInitialized = false;

        private void Awake()
        {
            Game.Messages.Subscribe<KerbalAddedToRoster>(OnNeedRefresh);
            Game.Messages.Subscribe<KerbalRemovedFromRoster>(OnNeedRefresh);
            Game.Messages.Subscribe<KerbalLocationChanged>(OnNeedRefresh);
            Game.Messages.Subscribe<VesselChangedMessage>(OnNeedRefresh);
        }

        private void OnDestroy()
        {
            
            Game.Messages.Unsubscribe<KerbalAddedToRoster>(OnNeedRefresh);
            Game.Messages.Unsubscribe<KerbalRemovedFromRoster>(OnNeedRefresh);
            Game.Messages.Unsubscribe<KerbalLocationChanged>(OnNeedRefresh);
            Game.Messages.Unsubscribe<VesselChangedMessage>(OnNeedRefresh);
        }
        
        private void Update()
        {
            if (!IsWindowOpen) return;
            
            _roster = Game.SessionManager.KerbalRosterManager;
            
            if (!_rosterInitialized)
                RefreshRoster();
        }
        
        private void OnEnable()
        {
            Enable();
            
            _addRandomKerbal = RootElement.Q<Button>("add-random-kerbal");
            _addRandomKerbal.clicked += AddRandomKerbal;
            
            _resetKerbalRoster = RootElement.Q<Button>("reset-kerbal-roster");
            _resetKerbalRoster.clicked += ResetRoster;
            
            _addByNameField = RootElement.Q<TextField>("add-by-name-field");
            _addByName = RootElement.Q<Button>("add-by-name");
            _addByName.clicked += AddKerbalByName;
            
            _addXKerbalsField = RootElement.Q<TextField>("add-x-field");
            _addXKerbals = RootElement.Q<Button>("add-x");
            _addXKerbals.clicked += AddXKerbals;
            
            _kerbals = RootElement.Q<ScrollView>("roster");
        }

        private void RefreshRoster()
        {
            _roster = Game.SessionManager.KerbalRosterManager;
            
            if (_kerbals == null || _roster == null) return;
            
            _kerbals.Clear();

            var row = new KerbalRosterRow(true);
            _kerbals.Add(row);
            
            foreach (var kerbal in _roster.GetAllKerbals())
            {
                row = new KerbalRosterRow();
                row.KerbalName.text = kerbal.Attributes.GetFullName();
                row.SimObject.text = kerbal.Location.SimObjectId.Guid.ToString().Split("-")[0];
                row.Seat.text = kerbal.Location.PositionIndex.ToString();
                row.EnrollmentDate.text = kerbal.EnrollmentUT.ToString("0");
                row.OnDelete = (Action<string>)Delegate.Combine(row.OnDelete, new Action<string>(DeleteKerbal));
                _kerbals.Add(row);
            }
            
            _rosterInitialized = true;
        }
        
        private void DeleteKerbal(string kerbalName)
        {
            if (_roster == null || !_roster.TryGetKerbalByName(kerbalName, out var kerbal)) return;
            
            _roster.DestroyKerbal(kerbal.Id);
        }

        private void OnNeedRefresh(MessageCenterMessage msg)
        {
            RefreshRoster();
        }

        private void AddRandomKerbal()
        {
            _roster?.CreateKerbal(Game.UniverseModel.UniverseTime);
        }

        private void ResetRoster()
        {
            if (_roster == null) return;
            
            var allKerbals = _roster.GetAllKerbals();
            for (var num = allKerbals.Count; num > 0; num--)
            {
                _roster.DestroyKerbal(allKerbals[num - 1].Id);
            }
        }

        private void AddKerbalByName()
        {
            if (_addByNameField == null) return;
            
            _roster?.CreateKerbalByName(Game.UniverseModel.UniverseTime, _addByNameField.value);
        }

        private void AddXKerbals()
        {
            var text = _addXKerbalsField?.text;
            if (!string.IsNullOrEmpty(text) && int.TryParse(text, out var result))
            {
                StartCoroutine(SpawnXKerbals(result));
            }
        }

        private IEnumerator SpawnXKerbals(int amountToSpawn)
        {
            for (var i = 0; i < amountToSpawn; i++)
            {
                var kerbal = _roster?.CreateKerbal(GameManager.Instance.Game.UniverseModel.UniverseTime);
                yield return new WaitUntil(() => kerbal != null);
            }
        }
    }
}