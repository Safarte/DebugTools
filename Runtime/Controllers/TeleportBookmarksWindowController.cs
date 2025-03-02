using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DebugTools.Runtime.UI;
using KSP.DebugTools;
using KSP.Game;
using KSP.IO;
using KSP.Messages;
using KSP.Sim.impl;
using KSP.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace DebugTools.Runtime.Controllers
{
    using BookmarksList = SortedList<string, TeleportBookmark>;

    public class TeleportBookmarksWindowController : BaseWindowController
    {
        private const string KSCBookmarkAddress = "Assets/Modules/DebugTools/Assets/KSCBookmark.json";
        private const string BuiltInBookmarksLabel = "debug_bookmarks";
        private const string TeleportBookmarksDir = "TeleportBookmarks";

        private Toggle? _autoSaveOnTeleport;
        private Button? _teleportToKSC;

        private TextField? _newListField;
        private Button? _createNewList;

        private DropdownField? _selectedList;
        private Button? _deleteList;
        private Button? _saveLists;

        private TextField? _newBookmarkName;
        private Button? _saveNewBookmark;

        private Button? _previousBookmark;
        private Button? _nextBookmark;

        private ScrollView? _teleportBookmarksView;
        private readonly List<TeleportBookmarkRow> _teleportBookmarks = new();
        private int _bookmarkIndex = -1;

        private TeleportBookmark _kscBookmark;
        private readonly Dictionary<string, BookmarksList> _bookmarksLists = new();

        private void Awake()
        {
            Game.Messages.Subscribe<GameLoadFinishedMessage>(OnGameLoadFinished);
        }

        private void OnDestroy()
        {
            Game.Messages.Unsubscribe<GameLoadFinishedMessage>(OnGameLoadFinished);
        }

        private void OnEnable()
        {
            Enable();

            _autoSaveOnTeleport = RootElement.Q<Toggle>("autosave-on-teleport");
            _autoSaveOnTeleport.value = false;

            _teleportToKSC = RootElement.Q<Button>("teleport-to-ksc");
            _teleportToKSC.clicked += OnTeleportToKSC;

            _newListField = RootElement.Q<TextField>("bookmarks-list-name");
            _createNewList = RootElement.Q<Button>("create-bookmarks-list");
            _createNewList.clicked += OnCreateNewList;

            _selectedList = RootElement.Q<DropdownField>("selected-list");
            _selectedList.RegisterValueChangedCallback(_ => PopulateBookmarks());

            _deleteList = RootElement.Q<Button>("delete-list");
            _deleteList.clicked += OnDeleteList;

            _saveLists = RootElement.Q<Button>("save-lists");
            _saveLists.clicked += OnSaveLists;

            _newBookmarkName = RootElement.Q<TextField>("save-bookmark-field");
            _saveNewBookmark = RootElement.Q<Button>("save-bookmark");
            _saveNewBookmark.clicked += OnSaveBookmark;

            _previousBookmark = RootElement.Q<Button>("previous-bookmark");
            _previousBookmark.clicked += OnPreviousBookmark;

            _nextBookmark = RootElement.Q<Button>("next-bookmark");
            _nextBookmark.clicked += OnNextBookmark;

            _teleportBookmarksView = RootElement.Q<ScrollView>("bookmarks");
        }

        /// <summary>
        /// Teleports to the Kerbal Space Center based on built-in teleport bookmark.
        /// </summary>
        private void OnTeleportToKSC()
        {
            var activeVehicle = Game.ViewController.GetActiveVehicle();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVehicle == null) return;

            if (_autoSaveOnTeleport != null && _autoSaveOnTeleport.value)
                Game.SaveLoadManager.ForceQuickSave();

            var guid = activeVehicle.Guid.ToString();
            Game.SpaceSimulation.TeleportSimObjectToSurface(guid, _kscBookmark.State);
        }

        /// <summary>
        /// Creates a new bookmarks list
        /// </summary>
        private void OnCreateNewList()
        {
            if (_newListField == null) return;

            // Sanitize user-input list name as it will be used as a file name for storage
            var listName = MakeValidFileName(_newListField.value);
            
            _bookmarksLists.Add(listName, new BookmarksList());
            UpdateBookmarksListsDropdown(listName);
            
            // Refresh bookmarks
            _bookmarkIndex = -1;
            PopulateBookmarks();
        }

        /// <summary>
        /// Converts a string to a valid filename
        /// </summary>
        /// <param name="name">Input string</param>
        /// <returns>Valid filename</returns>
        private static string MakeValidFileName(string name)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }

        /// <summary>
        /// Delete the selected bookmarks list (if more than 1 remains)
        /// </summary>
        private void OnDeleteList()
        {
            if (_selectedList == null || _bookmarksLists.Count <= 1) return;

            _bookmarksLists.Remove(_selectedList.value);
            UpdateBookmarksListsDropdown();
            
            // Refresh bookmarks
            _bookmarkIndex = -1;
            PopulateBookmarks();
        }

        /// <summary>
        /// Saves the current bookmarks list to persistent storage
        /// </summary>
        private void OnSaveLists()
        {
            foreach (var item in _bookmarksLists)
            {
                // Name of the JSON file is set the bookmarks list name
                var listPath = Path.Combine(Application.persistentDataPath, TeleportBookmarksDir, item.Key + ".json");

                var listDir = Path.GetDirectoryName(listPath);
                if (listDir == null) continue;
                
                // Create `TeleportBookmarks` directory if not exists
                if (!IOProvider.DirectoryExists(listDir))
                    IOProvider.CreateDirectory(listDir);

                // Save bookmarks as JSON array
                var bookmarks = _bookmarksLists[item.Key].Values.ToArray();
                IOProvider.ToJsonFile(listPath, bookmarks);
            }
        }

        /// <summary>
        /// Save a new bookmark to the selected list based on the active vessel's situation
        /// </summary>
        private void OnSaveBookmark()
        {
            if (_selectedList == null || _newBookmarkName == null) return;

            // If no name is provided, default to current universal time
            var bookmarkName = _newBookmarkName.value == ""
                ? Game.UniverseModel.UniverseTime.ToString("0")
                : _newBookmarkName.value;

            _bookmarksLists[_selectedList.value].Add(bookmarkName, GetCurrentLocationAsBookmark(bookmarkName));
            PopulateBookmarks();
        }

        /// <summary>
        /// Gets a bookmark from the active vessel's situation (orbit or surface).
        /// </summary>
        /// <param name="bookmarkName">Name of the bookmark</param>
        /// <returns>Filled teleport bookmark</returns>
        /// <exception cref="Exception">Thrown when vessel is in an invalid situation (escaping or unknown)</exception>
        private TeleportBookmark GetCurrentLocationAsBookmark(string bookmarkName)
        {
            // Get active vessel
            var activeVessel = Game.ViewController.GetActiveSimVessel();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null) throw new Exception("No active vessel found");

            var bookmark = new TeleportBookmark
            {
                BookmarkName = bookmarkName
            };

            switch (activeVessel.Situation)
            {
                case VesselSituations.Orbiting:
                    // Orbit-type bookmark
                    FillOrbitBookmarkState(activeVessel, ref bookmark);
                    break;
                case not VesselSituations.Escaping:
                    // Surface-type bookmark
                    FillSurfaceBookmarkState(activeVessel, ref bookmark);
                    break;
                default:
                    DebugToolsPlugin.Logger.LogError("Invalid teleport situation");
                    break;
            }

            return bookmark;
        }

        /// <summary>
        /// Set the passed bookmark's state based on the provided vessel's orbital situation.
        /// </summary>
        /// <param name="vessel">Vessel to base the teleportation state on</param>
        /// <param name="bookmark">Teleport bookmark to add the state to</param>
        private static void FillOrbitBookmarkState(VesselComponent vessel, ref TeleportBookmark bookmark)
        {
            bookmark.Type = BookmarkType.Orbit;
            bookmark.OrbitState = GetOrbitTeleportState(vessel);
        }

        /// <summary>
        /// Gets the orbit teleportation state from the current situation of the provided vessel.
        /// </summary>
        /// <param name="vessel">Vessel to base the teleportation state on</param>
        /// <returns>Orbit teleportation state</returns>
        private static KeplerOrbitState GetOrbitTeleportState(VesselComponent vessel)
        {
            var state = default(KeplerOrbitState);

            state.referenceBodyGuid = vessel.mainBody.Guid;
            state.inclination = vessel.Orbit.inclination;
            state.eccentricity = vessel.Orbit.eccentricity;
            state.semiMajorAxis = vessel.Orbit.semiMajorAxis;
            state.longitudeOfAscendingNode = vessel.Orbit.longitudeOfAscendingNode;
            state.argumentOfPeriapsis = vessel.Orbit.argumentOfPeriapsis;
            state.meanAnomalyAtEpoch = vessel.Orbit.meanAnomalyAtEpoch;
            state.epoch = vessel.Orbit.epoch;

            return state;
        }

        /// <summary>
        /// Set the passed bookmark's state based on the provided vessel's surface situation.
        /// </summary>
        /// <param name="vessel">Vessel to base the teleportation state on</param>
        /// <param name="bookmark">Teleport bookmark to add the state to</param>
        private static void FillSurfaceBookmarkState(VesselComponent vessel, ref TeleportBookmark bookmark)
        {
            bookmark.Type = BookmarkType.Surface;
            bookmark.State = GetSurfaceTeleportState(vessel);
        }

        /// <summary>
        /// Gets the surface teleportation state from the current situation of the provided vessel.
        /// </summary>
        /// <param name="vessel">Vessel to base the teleportation state on</param>
        /// <returns>Surface teleportation state</returns>
        private static SurfaceTeleportState GetSurfaceTeleportState(VesselComponent vessel)
        {
            var state = default(SurfaceTeleportState);
            state.ReferenceBodyGuid = vessel.mainBody.Guid;
            state.Altitude = vessel.AltitudeFromSurface;
            state.Latitude = vessel.Latitude;
            state.Longitude = vessel.Longitude;
            state.VerticalSpeed = vessel.VerticalSrfSpeed;

            return state;
        }

        /// <summary>
        /// Teleports to the previous bookmark in the selected list (cyclic behavior).
        /// </summary>
        private void OnPreviousBookmark()
        {
            if (_selectedList == null) return;

            var numBookmarks = _bookmarksLists[_selectedList.value].Count;
            _bookmarkIndex = (--_bookmarkIndex % numBookmarks + numBookmarks) % numBookmarks; // Cycle index

            TeleportToBookmark(_bookmarksLists[_selectedList.value].Keys[_bookmarkIndex]);
        }

        /// <summary>
        /// Teleports to the next bookmark in the selected list (cyclic behavior).
        /// </summary>
        private void OnNextBookmark()
        {
            if (_selectedList == null) return;

            var numBookmarks = _bookmarksLists[_selectedList.value].Count;
            _bookmarkIndex = (++_bookmarkIndex % numBookmarks + numBookmarks) % numBookmarks; // Cycle index

            TeleportToBookmark(_bookmarksLists[_selectedList.value].Keys[_bookmarkIndex]);
        }

        /// <summary>
        /// Loads all bookmarks when the game finishes loading a campaign.
        /// </summary>
        private void OnGameLoadFinished(MessageCenterMessage msg)
        {
            GameManager.Instance.Assets.Load<TextAsset>(KSCBookmarkAddress, OnKSCBookmarkLoaded);
            PopulateBookmarksLists();
        }

        /// <summary>
        /// Loads the KSC bookmark from the <c>KSCBookmark.json</c> file in <c>Assets/Modules/DebugTools/Assets</c>.
        /// </summary>
        /// <param name="asset">KSC Bookmark text asset</param>
        private void OnKSCBookmarkLoaded(TextAsset asset)
        {
            _kscBookmark = IOProvider.FromJson<TeleportBookmark>(asset.text);
        }

        /// <summary>
        /// Populates the bookmarks lists from built-in addressable JSON arrays with the <c>debug_bookmarks</c> label or
        /// from persistent storage. Lists from persistent storage will overwrite built-in lists.
        /// </summary>
        private void PopulateBookmarksLists()
        {
            if (_selectedList == null) return;

            _bookmarksLists.Clear();

            GameManager.Instance.Assets.LoadByLabel<TextAsset>(BuiltInBookmarksLabel, OnBuiltInBookmarkLoaded, delegate
            {
                LoadSavedBookmarksLists();
                UpdateBookmarksListsDropdown();
                PopulateBookmarks();
            });
        }

        /// <summary>
        /// Updates the choices in the bookmarks list dropdown and optionally set it to a specific value
        /// </summary>
        /// <param name="startingList">Optional starting bookmarks list</param>
        private void UpdateBookmarksListsDropdown(string? startingList = null)
        {
            if (_selectedList == null) return;

            _selectedList.choices.Clear();
            _selectedList.choices.AddRange(_bookmarksLists.Keys);

            if (startingList != null)
                _selectedList.value = startingList;
            else
                _selectedList.index = 0;
        }

        /// <summary>
        /// Loads a bookmarks list provided as a <c>TextAsset</c> JSON file.
        /// </summary>
        /// <param name="asset">JSON file</param>
        private void OnBuiltInBookmarkLoaded(TextAsset asset)
        {
            // Try parsing asset text as JSON array
            var bookmarks = IOProvider.FromJson<TeleportBookmark[]>(asset.text);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (bookmarks == null) return;

            AddBookmarksList(asset.name, bookmarks);
        }

        /// <summary>
        /// Loads the bookmarks lists that were saved to persistent storage as JSON files.
        /// </summary>
        private void LoadSavedBookmarksLists()
        {
            var bookmarksPath = Path.Combine(Application.persistentDataPath, TeleportBookmarksDir);
            if (!IOProvider.DirectoryExists(bookmarksPath)) return;

            var files = IOProvider.GetFilesNamesFromDirectory(bookmarksPath, "*", false);
            foreach (var file in files)
            {
                try
                {
                    // Try parsing file as a JSON array
                    if (IOProvider.FromJsonFile<TeleportBookmark[]>(file, out var bookmarks))
                        AddBookmarksList(Path.GetFileNameWithoutExtension(file), bookmarks);
                }
                catch (Exception e)
                {
                    DebugToolsPlugin.Logger.LogError(e.Message);
                }
            }
        }

        /// <summary>
        /// Adds a new <c>BookmarksList</c> from an array of bookmarks
        /// </summary>
        /// <param name="listName">Name of the bookmarks list</param>
        /// <param name="bookmarks">Array of teleport bookmarks</param>
        private void AddBookmarksList(string listName, TeleportBookmark[] bookmarks)
        {
            var list = new BookmarksList();

            foreach (var bookmark in bookmarks)
            {
                list[bookmark.BookmarkName] = bookmark;
            }

            _bookmarksLists[listName] = list;
        }

        /// <summary>
        /// Populates the scrollable list of teleport bookmark rows with the bookmarks in the selected bookmarks list.
        /// </summary>
        private void PopulateBookmarks()
        {
            if (_teleportBookmarksView == null || _selectedList == null) return;

            _teleportBookmarksView.Clear();
            _teleportBookmarks.Clear();

            foreach (var bookmark in _bookmarksLists[_selectedList.value].Values)
            {
                var row = new TeleportBookmarkRow();
                row.Name.text = bookmark.BookmarkName;
                row.BodyName.text = bookmark.GetBodyName();
                row.Type.text = bookmark.Type.ToString();
                row.OnTeleport = TeleportToBookmark;
                row.OnDelete = DeleteBookmark;

                _teleportBookmarks.Add(row);
                _teleportBookmarksView.Add(row);
            }

            UpdateSelectedBookmark();
        }

        /// <summary>
        /// Updates which bookmark row has the "selected" visual cue
        /// </summary>
        private void UpdateSelectedBookmark()
        {
            var i = 0;
            foreach (var row in _teleportBookmarks)
            {
                if (i++ == _bookmarkIndex)
                {
                    row.SetSelected(true);
                    _teleportBookmarksView?.ScrollTo(row);
                }
                else
                    row.SetSelected(false);
            }
        }

        /// <summary>
        /// Teleports the active vessel to the required bookmark from the selected bookmarks list. Auto-saves the game
        /// if the "Auto-save on Teleport" toggle is on.
        /// </summary>
        /// <param name="bookmarkName">Name of the bookmark</param>
        /// <exception cref="NotImplementedException">Thrown if the bookmark's type is invalid</exception>
        private void TeleportToBookmark(string bookmarkName)
        {
            if (_selectedList == null) return;

            // Get teleport bookmark
            var bookmark = _bookmarksLists[_selectedList.value][bookmarkName];

            // Select teleported bookmark
            _bookmarkIndex = _bookmarksLists[_selectedList.value].IndexOfKey(bookmarkName);
            UpdateSelectedBookmark();

            // Get active vessel GUID
            var activeVessel = Game.ViewController.GetActiveVehicle();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (activeVessel == null) return;
            var vesselGUID = activeVessel.Guid.ToString();

            // Auto-save if required
            if (_autoSaveOnTeleport != null && _autoSaveOnTeleport.value)
                Game.SaveLoadManager.ForceQuickSave();

            switch (bookmark.Type)
            {
                case BookmarkType.Orbit:
                    bookmark.OrbitState.epoch += Game.UniverseModel.UniverseTime;
                    Game.SpaceSimulation.TeleportSimObjectToOrbit(vesselGUID, bookmark.OrbitState, true);
                    break;
                case BookmarkType.Surface:
                    Game.SpaceSimulation.TeleportSimObjectToSurface(vesselGUID, bookmark.State);
                    break;
                default:
                    throw new NotImplementedException("Unknown bookmark type " + bookmark.Type);
            }
        }

        /// <summary>
        /// Deletes a bookmark from the selected list and refreshes bookmarks rows.
        /// </summary>
        /// <param name="bookmarkName">Name of the bookmark</param>
        private void DeleteBookmark(string bookmarkName)
        {
            if (_selectedList == null) return;

            _bookmarksLists[_selectedList.value].Remove(bookmarkName);
            PopulateBookmarks();
        }

        private void Update()
        {
            if (!IsWindowOpen) return;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            var hasActiveVessel = Game.ViewController.GetActiveVehicle() != null;
            _saveNewBookmark?.SetEnabled(hasActiveVessel);

            var canTeleport = hasActiveVessel && Game.UniverseModel.TimeScale <= 1f;
            _teleportToKSC?.SetEnabled(canTeleport);

            var canTeleportToBookmark = canTeleport && _teleportBookmarks.Count > 0;
            _previousBookmark?.SetEnabled(canTeleportToBookmark);
            _nextBookmark?.SetEnabled(canTeleportToBookmark);

            var canDeleteList = _bookmarksLists.Count > 1;
            _deleteList?.SetEnabled(canDeleteList);
        }
    }
}