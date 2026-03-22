using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spelunky {

    public enum RoomPoolSource {
        Missing,
        StageDefinition,
        LegacyFallback
    }

    public enum StageDefinitionSource {
        Missing,
        RuntimeOverride,
        StageNumberAndId,
        StageIdOnly,
        StageNumberOnly,
        ArrayFallback
    }

    /// <summary>
    /// Generates our levels.
    /// TODO: Currently extremely wip. Only generates a single level and very badly.
    /// </summary>
    public class LevelGenerator : MonoBehaviour {

        private const int LegacyTrapRoomIndex = 0;
        private const int LegacySacrificeRoomIndex = 1;
        private static readonly string[] ImmediateHazardNameHints = {
            "ArrowTrap",
            "Spikes",
            "DamageArea",
            "DamagingBlock"
        };

        [Header("Debug")]
        public bool debug;

        public SpriteRenderer boundsStraight;
        public SpriteRenderer boundsCorner;

        public GameObject circle;
        public GameObject arrowRight;
        public GameObject arrowDown;
        public GameObject arrowLeft;

        public int roomsHorizontal = 4;
        public int roomsVertical = 4;

        [Header("Legacy room pools")]
        public Room[] normalRooms;
        public Room[] specialRooms;

        [Header("Stage data")]
        [SerializeField] private StageDefinition[] stageDefinitions;
        [Tooltip("Temporary stage number override. 0 is treated as Stage 1 for legacy scenes.")]
        [SerializeField] private int currentStageIndex;
        [SerializeField] private bool syncStageNumberFromRunManager = true;
        [SerializeField, Min(1)] private int expectedStageCount = 4;
        [SerializeField, TextArea(3, 8)] private string activeStageConfigurationSummary;

        public Room[,] Rooms { get; private set; }
        public Tile[,] Tiles { get; private set; }

        // The width and height of the rooms in number of tiles.
        // I left these at the default Spelunky values so that it's easy to recreate the same rooms here if desirable.
        // NB: Changing these involves recreating all room prefabs.
        public const int RoomWidth = 10;
        public const int RoomHeight = 8;

        // The total pixel width and height of the level.
        public float LevelWidth {
            get { return RoomWidth * ActiveRoomsHorizontal * Tile.Width; }
        }

        public float LevelHeight {
            get { return RoomHeight * ActiveRoomsVertical * Tile.Height; }
        }

        private Dictionary<string, Tile> _tilePrefabs;
        private Dictionary<string, GameObject> _backgroundPrefabs;

        private Transform _boundsParent;
        private Transform _backgroundParent;
        private Transform _roomParent;
        private Transform _debugParent;

        private Vector2 _direction;
        private Vector2 _lastDirection;

        private Room firstRoom;
        private Room lastRoom;
        [HideInInspector] public Tile entrance;
        [HideInInspector] public Tile exit;

        private int _spawnedTrapRoomCount;
        private int _spawnedSacrificalAltarCount;

        private StageDefinition _runtimeStageOverride;
        private StageDefinition _activeStageDefinition;
        private StageDefinitionSource _activeStageDefinitionSource;
        private int _activeRoomsHorizontal;
        private int _activeRoomsVertical;
        private Room[] _activeNormalRooms;
        private readonly Dictionary<SpecialRoomType, Room[]> _activeSpecialRoomPools = new Dictionary<SpecialRoomType, Room[]>();
        private RoomPoolSource _activeNormalRoomSource;
        private readonly Dictionary<SpecialRoomType, RoomPoolSource> _activeSpecialRoomSources = new Dictionary<SpecialRoomType, RoomPoolSource>();
        private readonly List<GameObject> _generatedLevelObjects = new List<GameObject>();
        private readonly HashSet<AccessoryType> _spawnedPermanentAccessories = new HashSet<AccessoryType>();
        private Tile _generatedEntrance;
        private Tile _generatedExit;

        public static LevelGenerator instance;
        public StageDefinition ActiveStageDefinition => _activeStageDefinition;
        public int ActiveStageNumber => _activeStageDefinition != null ? _activeStageDefinition.stageNumber : ResolveRequestedStageNumber();
        public int ActiveRoomsHorizontal => _activeRoomsHorizontal > 0 ? _activeRoomsHorizontal : Mathf.Max(1, roomsHorizontal);
        public int ActiveRoomsVertical => _activeRoomsVertical > 0 ? _activeRoomsVertical : Mathf.Max(1, roomsVertical);
        public string ActiveStageConfigurationSummary => activeStageConfigurationSummary;
        public bool HasGeneratedLevelContent => GetTrackedGeneratedObjectCount() > 0;

        public bool TryGetConfiguredStageDefinition(int stageNumber, out StageDefinition stageDefinition) {
            stageDefinition = null;
            if (stageDefinitions == null || stageDefinitions.Length == 0 || stageNumber < 1) {
                return false;
            }

            foreach (StageDefinition configuredStageDefinition in stageDefinitions) {
                if (configuredStageDefinition == null || !configuredStageDefinition.MatchesStageNumber(stageNumber)) {
                    continue;
                }

                stageDefinition = configuredStageDefinition;
                return true;
            }

            return false;
        }

        private void Awake() {
            instance = this;

            UnityEngine.Object[] resourcesTiles = Resources.LoadAll("Tiles/Prefabs", typeof(Tile));
            _tilePrefabs = new Dictionary<string, Tile>();
            foreach (UnityEngine.Object resource in resourcesTiles) {
                Tile tile = (Tile)resource;
                _tilePrefabs.Add(tile.name, tile);
            }

            UnityEngine.Object[] resourcesBackgrounds = Resources.LoadAll("Backgrounds/Prefabs", typeof(GameObject));
            _backgroundPrefabs = new Dictionary<string, GameObject>();
            foreach (UnityEngine.Object resource in resourcesBackgrounds) {
                GameObject background = (GameObject)resource;
                _backgroundPrefabs.Add(background.name, background);
            }

            _boundsParent = GameObject.Find("_BOUNDS").GetComponent<Transform>();
            _backgroundParent = GameObject.Find("_BACKGROUND").GetComponent<Transform>();
            _roomParent = GameObject.Find("_ROOMS").GetComponent<Transform>();
            _debugParent = GameObject.Find("_DEBUG").GetComponent<Transform>();

            RefreshStageConfiguration();
            AllocateRuntimeArrays();
        }

        private void OnValidate() {
            if (currentStageIndex < 0) {
                currentStageIndex = 0;
            }

            ValidateStageDefinitions();
        }

        /// <summary>
        /// Procedurally generates rooms and main path.
        /// Call this for normal gameplay. Skip this for testing scenes with hand-placed content.
        /// </summary>
        public void GenerateLevel() {
            RefreshStageConfiguration();
            AllocateRuntimeArrays();

            // 1. First create the main path from entrance to exit.
            CreateMainPathRooms();

            // 2. Then create any rooms not on the main path.
            CreateRemainingRooms();
        }

        /// <summary>
        /// Sets up the level (tiles, bounds, background).
        /// Always call this after GenerateLevel() or after hand-placing content.
        /// </summary>
        public void SetupLevel() {
            // 1. Setup the tiles (add variations, decorations etc.)
            InitializeTiles();
            SetupTiles();

            // 2. Create the indestructible bounds around the level.
            CreateLevelBounds();

            // 3. Create the background sprites.
            CreateBackground();
        }

        /// <summary>
        /// Places the entrance and exit tiles. Must be called after SetupLevel()
        /// since it needs initialized tiles to find suitable placement spots.
        /// Skip this for testing scenes where entrance/exit are hand-placed.
        /// </summary>
        public void PlaceEntranceAndExit() {
            if (!TryGetEntranceExitSpawnTile(firstRoom, "Entrance", out Tile tileToSpawnEntranceOn)) {
                return;
            }

            entrance = Instantiate(_tilePrefabs["Entrance"], tileToSpawnEntranceOn.transform.position + new Vector3(0, Tile.Height, 0), Quaternion.identity);
            _generatedEntrance = entrance;
            TrackGeneratedObject(entrance.gameObject);

            if (!TryGetEntranceExitSpawnTile(lastRoom, "Exit", out Tile tileToSpawnExitOn)) {
                return;
            }

            exit = Instantiate(_tilePrefabs["Exit"], tileToSpawnExitOn.transform.position + new Vector3(0, Tile.Height, 0), Quaternion.identity);
            _generatedExit = exit;
            TrackGeneratedObject(exit.gameObject);

            ApplyEntranceSafetyRules();
            SanitizePermanentAccessoryPickupsInScene();
        }

        public void SetStageDefinition(StageDefinition stageDefinition) {
            _runtimeStageOverride = stageDefinition;
            RefreshStageConfiguration();
        }

        public bool SetStageIndex(int stageIndex) {
            return SetStageNumber(stageIndex);
        }

        public bool SetStageNumber(int stageNumber) {
            if (stageNumber < 1) {
                return false;
            }

            currentStageIndex = stageNumber;
            _runtimeStageOverride = null;
            RefreshStageConfiguration();
            return _activeStageDefinition != null;
        }

        /// <summary>
        /// Clears procedurally generated content so the same scene can generate another stage.
        /// Only tracked generator-spawned objects are removed, which keeps hand-placed test scenes safe.
        /// </summary>
        public void ClearGeneratedLevel() {
            for (int i = _generatedLevelObjects.Count - 1; i >= 0; i--) {
                GameObject generatedObject = _generatedLevelObjects[i];
                if (generatedObject == null) {
                    continue;
                }

                generatedObject.SetActive(false);
                DestroyTrackedObject(generatedObject);
            }

            _generatedLevelObjects.Clear();
            ResetLevelRuntime();
        }

        /// <summary>
        /// Clears runtime-only references and arrays without touching hand-placed scene content.
        /// This is the lightweight companion to ClearGeneratedLevel() for in-place stage transitions.
        /// </summary>
        public void ResetLevelRuntime() {
            RefreshStageConfiguration();
            AllocateRuntimeArrays();

            firstRoom = null;
            lastRoom = null;
            _spawnedTrapRoomCount = 0;
            _spawnedSacrificalAltarCount = 0;
            _direction = Vector2.zero;
            _lastDirection = Vector2.zero;

            if (entrance == _generatedEntrance) {
                entrance = null;
            }

            if (exit == _generatedExit) {
                exit = null;
            }

            _generatedEntrance = null;
            _generatedExit = null;
            _spawnedPermanentAccessories.Clear();
            activeStageConfigurationSummary = BuildActiveStageConfigurationSummary();
        }

        [ContextMenu("Refresh Stage Configuration")]
        private void RefreshStageConfigurationFromContextMenu() {
            RefreshStageConfiguration();
        }

        [ContextMenu("Log Active Stage Configuration")]
        private void LogActiveStageConfiguration() {
            RefreshStageConfiguration();
            Debug.Log($"LevelGenerator: {activeStageConfigurationSummary}");
        }

        [ContextMenu("Clear Generated Level")]
        private void ClearGeneratedLevelFromContextMenu() {
            ClearGeneratedLevel();
        }

        /// <summary>
        /// </summary>
        private void CreateMainPathRooms() {
            Vector2 currentIndex = new Vector2(Random.Range(0, Rooms.GetLength(0)), Rooms.GetLength(1) - 1);
            PickRandomDirection();

            firstRoom = null;
            lastRoom = null;
            bool stopGeneration = false;
            while (!stopGeneration) {
                Vector2 indexToCheck = new Vector2((int)currentIndex.x + (int)_direction.x, (int)currentIndex.y + (int)_direction.y);
                // Out of bounds.
                if (indexToCheck.x < 0 || indexToCheck.x >= Rooms.GetLength(0)) {
                    _lastDirection = _direction;
                    _direction = Vector2.down;
                }
                // Reached the bottom row.
                else if (indexToCheck.y < 0) {
                    if (firstRoom == null) {
                        _lastDirection = Vector2.zero;
                    }

                    _direction = Vector2.zero;

                    Room roomToSpawn = FindSuitableRoom(currentIndex);
                    if (roomToSpawn == null) {
                        Debug.LogError("No suitable main path room found. Trying to find any room instead.");
                        roomToSpawn = FindAnyRoom();
                    }

                    if (roomToSpawn == null) {
                        Debug.LogError("No room found at all!");
                    }

                    Room spawnedRoom = SpawnRoom(roomToSpawn, currentIndex);
                    if (firstRoom == null) {
                        firstRoom = spawnedRoom;
                    }

                    InstantiateDirectionArrow(currentIndex);
                    currentIndex = indexToCheck;

                    stopGeneration = true;

                    lastRoom = spawnedRoom;
                }
                // Found an empty slot.
                else if (Rooms[(int)indexToCheck.x, (int)indexToCheck.y] == null) {
                    if (firstRoom == null) {
                        _lastDirection = Vector2.zero;
                    }

                    Room roomToSpawn = FindSuitableRoom(currentIndex);
                    if (roomToSpawn == null) {
                        Debug.LogError("No suitable main path room found. Trying to find any room instead.");
                        roomToSpawn = FindAnyRoom();
                    }

                    if (roomToSpawn == null) {
                        Debug.LogError("No room found at all!");
                    }

                    Room spawnedRoom = SpawnRoom(roomToSpawn, currentIndex);
                    if (firstRoom == null) {
                        firstRoom = spawnedRoom;
                    }

                    InstantiateDirectionArrow(currentIndex);
                    currentIndex = indexToCheck;

                    PickRandomDirection();
                }
                // If all else fails try again with a different direction.
                else {
                    PickRandomDirection();
                }
            }
        }

        /// <summary>
        /// </summary>
        private void CreateRemainingRooms() {
            for (int x = 0; x < Rooms.GetLength(0); x++) {
                for (int y = 0; y < Rooms.GetLength(1); y++) {
                    Vector2 currentIndex = new Vector2(x, y);
                    if (Rooms[(int)currentIndex.x, (int)currentIndex.y] == null) {
                        Room roomToSpawn = null;
                        if (_spawnedTrapRoomCount < GetTrapRoomLimit() && Random.value < GetTrapRoomChance()) {
                            roomToSpawn = GetRandomSpecialRoom(SpecialRoomType.Trap);
                            if (roomToSpawn != null) {
                                _spawnedTrapRoomCount++;
                            }
                        }
                        else if (_spawnedSacrificalAltarCount < GetSacrificeRoomLimit() && Random.value < GetSacrificeRoomChance()) {
                            roomToSpawn = GetRandomSpecialRoom(SpecialRoomType.Sacrifice);
                            if (roomToSpawn != null) {
                                _spawnedSacrificalAltarCount++;
                            }
                        }

                        if (roomToSpawn == null) {
                            roomToSpawn = FindAnyRoom();
                        }

                        SpawnRoom(roomToSpawn, currentIndex);
                        if (debug) {
                            TrackGeneratedObject(Instantiate(circle, CurrentPosition(currentIndex, true), Quaternion.identity, _debugParent));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="roomToSpawn"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        private Room SpawnRoom(Room roomToSpawn, Vector2 currentIndex) {
            Room roomInstance = Instantiate(roomToSpawn, CurrentPosition(currentIndex), Quaternion.identity, _roomParent);
            roomInstance.name = "Room [" + currentIndex.x + "," + currentIndex.y + "]";
            roomInstance.index = currentIndex;
            roomInstance.debug = debug;
            Rooms[(int)currentIndex.x, (int)currentIndex.y] = roomInstance;
            TrackGeneratedObject(roomInstance.gameObject);
            return roomInstance;
        }

        /// <summary>
        /// </summary>
        private void PickRandomDirection() {
            _lastDirection = _direction;
            PickRandomDirection(GetMainPathDownChance());
        }

        /// <summary>
        /// </summary>
        /// <param name="arrow"></param>
        /// <returns></returns>
        private static Vector3 CurrentPosition(Vector2 currentIndex, bool arrow = false) {
            if (arrow) {
                return new Vector3(currentIndex.x * RoomWidth * Tile.Width + RoomWidth * Tile.Width / 2f, currentIndex.y * RoomHeight * Tile.Height + RoomHeight * Tile.Height / 2f, 0);
            }

            return new Vector3(currentIndex.x * RoomWidth * Tile.Width, currentIndex.y * RoomHeight * Tile.Height, 0);
        }

        /// <summary>
        /// </summary>
        private void InstantiateDirectionArrow(Vector2 currentIndex) {
            if (!debug) {
                return;
            }

            if (_direction == Vector2.right) {
                TrackGeneratedObject(Instantiate(arrowRight, CurrentPosition(currentIndex, true), Quaternion.identity, _debugParent));
            }
            else if (_direction == Vector2.left) {
                TrackGeneratedObject(Instantiate(arrowLeft, CurrentPosition(currentIndex, true), Quaternion.identity, _debugParent));
            }
            else {
                TrackGeneratedObject(Instantiate(arrowDown, CurrentPosition(currentIndex, true), Quaternion.identity, _debugParent));
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Room FindSuitableRoom(Vector2 currentIndex) {
            bool top = _lastDirection == Vector2.down || _direction == Vector2.up;
            bool right = _lastDirection == Vector2.left || _direction == Vector2.right;
            bool down = _direction == Vector2.down;
            // TODO: This doesn't work. Don't spawn rooms with opening down if we're at the bottom.
            if (currentIndex.y == 0) {
                down = false;
            }

            bool left = _lastDirection == Vector2.right || _direction == Vector2.left;
            List<Room> suitableRooms = new List<Room>();
            if (!HasRooms(_activeNormalRooms)) {
                return null;
            }

            foreach (Room room in _activeNormalRooms) {
                if ((top && !room.top) ||
                    (right && !room.right) ||
                    (down && !room.down) ||
                    (left && !room.left)) {
                    continue;
                }

                suitableRooms.Add(room);
            }

            return suitableRooms.Count > 0 ? suitableRooms[Random.Range(0, suitableRooms.Count)] : null;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Room FindAnyRoom() {
            return GetRandomRoom(_activeNormalRooms);
        }

        private void RefreshStageConfiguration() {
            ValidateStageDefinitions();
            _activeStageDefinition = ResolveStageDefinition(out _activeStageDefinitionSource);
            _activeRoomsHorizontal = ResolveActiveRoomsHorizontal(_activeStageDefinition);
            _activeRoomsVertical = ResolveActiveRoomsVertical(_activeStageDefinition);
            _activeNormalRooms = ResolveNormalRooms(_activeStageDefinition, out _activeNormalRoomSource);

            _activeSpecialRoomPools.Clear();
            _activeSpecialRoomSources.Clear();
            _activeSpecialRoomPools[SpecialRoomType.Trap] = ResolveSpecialRooms(_activeStageDefinition, SpecialRoomType.Trap, out RoomPoolSource trapSource);
            _activeSpecialRoomPools[SpecialRoomType.Sacrifice] = ResolveSpecialRooms(_activeStageDefinition, SpecialRoomType.Sacrifice, out RoomPoolSource sacrificeSource);
            _activeSpecialRoomSources[SpecialRoomType.Trap] = trapSource;
            _activeSpecialRoomSources[SpecialRoomType.Sacrifice] = sacrificeSource;

            _spawnedTrapRoomCount = 0;
            _spawnedSacrificalAltarCount = 0;
            _direction = Vector2.zero;
            _lastDirection = Vector2.zero;
            activeStageConfigurationSummary = BuildActiveStageConfigurationSummary();
        }

        private int ResolveActiveRoomsHorizontal(StageDefinition stageDefinition) {
            if (stageDefinition != null) {
                return Mathf.Max(1, stageDefinition.roomGridWidth);
            }

            return Mathf.Max(1, roomsHorizontal);
        }

        private int ResolveActiveRoomsVertical(StageDefinition stageDefinition) {
            if (stageDefinition != null) {
                return Mathf.Max(1, stageDefinition.roomGridHeight);
            }

            return Mathf.Max(1, roomsVertical);
        }

        private StageDefinition ResolveStageDefinition(out StageDefinitionSource stageDefinitionSource) {
            if (_runtimeStageOverride != null) {
                stageDefinitionSource = StageDefinitionSource.RuntimeOverride;
                return _runtimeStageOverride;
            }

            if (stageDefinitions == null || stageDefinitions.Length == 0) {
                stageDefinitionSource = StageDefinitionSource.Missing;
                return null;
            }

            // Stage number mapping is 1-based. Legacy scenes with 0 still resolve to Stage 1.
            int requestedStageNumber = ResolveRequestedStageNumber();
            string requestedStageId = ResolveRequestedStageId();
            StageDefinition mappedStageDefinition = FindStageDefinition(requestedStageNumber, requestedStageId, out stageDefinitionSource);
            if (mappedStageDefinition != null) {
                return mappedStageDefinition;
            }

            int fallbackIndex = Mathf.Clamp(requestedStageNumber - 1, 0, stageDefinitions.Length - 1);
            stageDefinitionSource = StageDefinitionSource.ArrayFallback;
            return stageDefinitions[fallbackIndex];
        }

        private Room[] ResolveNormalRooms(StageDefinition stageDefinition, out RoomPoolSource roomPoolSource) {
            if (stageDefinition != null && HasRooms(stageDefinition.normalRooms)) {
                roomPoolSource = RoomPoolSource.StageDefinition;
                return stageDefinition.normalRooms;
            }

            // If a stage explicitly disables fallback, fail fast instead of silently reusing the legacy pool.
            if (stageDefinition != null && !stageDefinition.allowLegacyNormalRoomFallback) {
                roomPoolSource = RoomPoolSource.Missing;
                return null;
            }

            roomPoolSource = HasRooms(normalRooms) ? RoomPoolSource.LegacyFallback : RoomPoolSource.Missing;
            return normalRooms;
        }

        private Room[] ResolveSpecialRooms(StageDefinition stageDefinition, SpecialRoomType specialRoomType, out RoomPoolSource roomPoolSource) {
            if (stageDefinition != null) {
                Room[] stageSpecificRooms = stageDefinition.GetSpecialRoomPool(specialRoomType);
                if (HasRooms(stageSpecificRooms)) {
                    roomPoolSource = RoomPoolSource.StageDefinition;
                    return stageSpecificRooms;
                }
            }

            // Special rooms follow the same precedence as normal rooms: stage-specific pool first, legacy fallback second.
            if (stageDefinition != null && !stageDefinition.allowLegacySpecialRoomFallback) {
                roomPoolSource = RoomPoolSource.Missing;
                return null;
            }

            int legacyIndex = GetLegacySpecialRoomIndex(specialRoomType);
            if (specialRooms == null || legacyIndex < 0 || legacyIndex >= specialRooms.Length || specialRooms[legacyIndex] == null) {
                roomPoolSource = RoomPoolSource.Missing;
                return null;
            }

            roomPoolSource = RoomPoolSource.LegacyFallback;
            return new[] { specialRooms[legacyIndex] };
        }

        private Room GetRandomSpecialRoom(SpecialRoomType specialRoomType) {
            return _activeSpecialRoomPools.TryGetValue(specialRoomType, out Room[] roomPool) ? GetRandomRoom(roomPool) : null;
        }

        private static bool HasRooms(Room[] roomPool) {
            return roomPool != null && roomPool.Length > 0;
        }

        private static Room GetRandomRoom(Room[] roomPool) {
            if (!HasRooms(roomPool)) {
                return null;
            }

            return roomPool[Random.Range(0, roomPool.Length)];
        }

        private float GetTrapRoomChance() {
            return _activeStageDefinition != null ? _activeStageDefinition.trapRoomChance : 0.1f;
        }

        private float GetSacrificeRoomChance() {
            return _activeStageDefinition != null ? _activeStageDefinition.sacrificeRoomChance : 0.1f;
        }

        private int GetTrapRoomLimit() {
            return _activeStageDefinition != null ? Mathf.Max(0, _activeStageDefinition.maxTrapRooms) : 1;
        }

        private int GetSacrificeRoomLimit() {
            return _activeStageDefinition != null ? Mathf.Max(0, _activeStageDefinition.maxSacrificeRooms) : 1;
        }

        private float GetMainPathDownChance() {
            if (_activeStageDefinition != null) {
                return Mathf.Clamp01(_activeStageDefinition.mainPathDownChance);
            }

            return 0.2f;
        }

        private StageMainPathStyle GetMainPathStyle() {
            return _activeStageDefinition != null ? _activeStageDefinition.mainPathStyle : StageMainPathStyle.Classic;
        }

        private int ResolveRequestedStageNumber() {
            if (syncStageNumberFromRunManager && RunManager.Instance != null && RunManager.Instance.CurrentRun != null) {
                return Mathf.Max(1, RunManager.Instance.CurrentRun.currentStageIndex);
            }

            return currentStageIndex <= 0 ? 1 : currentStageIndex;
        }

        private string ResolveRequestedStageId() {
            if (syncStageNumberFromRunManager && RunManager.Instance != null && RunManager.Instance.CurrentRun != null) {
                return RunManager.Instance.CurrentRun.currentStageId;
            }

            return null;
        }

        private StageDefinition FindStageDefinition(int stageNumber, string stageId, out StageDefinitionSource stageDefinitionSource) {
            foreach (StageDefinition stageDefinition in stageDefinitions) {
                if (stageDefinition == null || !stageDefinition.MatchesStageRequest(stageNumber, stageId)) {
                    continue;
                }

                stageDefinitionSource = !string.IsNullOrWhiteSpace(stageId)
                    ? StageDefinitionSource.StageNumberAndId
                    : StageDefinitionSource.StageNumberOnly;
                return stageDefinition;
            }

            if (!string.IsNullOrWhiteSpace(stageId)) {
                foreach (StageDefinition stageDefinition in stageDefinitions) {
                    if (stageDefinition == null || !stageDefinition.MatchesStageId(stageId)) {
                        continue;
                    }

                    stageDefinitionSource = StageDefinitionSource.StageIdOnly;
                    return stageDefinition;
                }
            }

            foreach (StageDefinition stageDefinition in stageDefinitions) {
                if (stageDefinition == null || !stageDefinition.MatchesStageNumber(stageNumber)) {
                    continue;
                }

                stageDefinitionSource = StageDefinitionSource.StageNumberOnly;
                return stageDefinition;
            }

            stageDefinitionSource = StageDefinitionSource.Missing;
            return null;
        }

        private void ValidateStageDefinitions() {
            if (stageDefinitions == null || stageDefinitions.Length == 0) {
                return;
            }

            Dictionary<int, StageDefinition> seenStageNumbers = new Dictionary<int, StageDefinition>();
            Dictionary<string, StageDefinition> seenStageIds = new Dictionary<string, StageDefinition>(StringComparer.OrdinalIgnoreCase);
            int highestStageNumber = 0;
            foreach (StageDefinition stageDefinition in stageDefinitions) {
                if (stageDefinition == null) {
                    continue;
                }

                highestStageNumber = Mathf.Max(highestStageNumber, stageDefinition.stageNumber);
                if (seenStageNumbers.TryGetValue(stageDefinition.stageNumber, out StageDefinition existingStageNumberDefinition)) {
                    Debug.LogWarning(
                        $"LevelGenerator: Duplicate StageDefinition mapping for stage {stageDefinition.stageNumber}. " +
                        $"Existing '{GetStageDefinitionLabel(existingStageNumberDefinition)}', duplicate '{GetStageDefinitionLabel(stageDefinition)}'."
                    );
                }
                else {
                    seenStageNumbers.Add(stageDefinition.stageNumber, stageDefinition);
                }

                if (string.IsNullOrWhiteSpace(stageDefinition.stageId)) {
                    Debug.LogWarning($"LevelGenerator: Stage {stageDefinition.stageNumber} is missing a stageId.");
                }
                else if (seenStageIds.TryGetValue(stageDefinition.stageId, out StageDefinition existingStageIdDefinition)) {
                    Debug.LogWarning(
                        $"LevelGenerator: Duplicate StageDefinition mapping for stageId '{stageDefinition.stageId}'. " +
                        $"Existing '{GetStageDefinitionLabel(existingStageIdDefinition)}', duplicate '{GetStageDefinitionLabel(stageDefinition)}'."
                    );
                }
                else {
                    seenStageIds.Add(stageDefinition.stageId, stageDefinition);
                }

                if (!stageDefinition.HasNormalRooms() && !stageDefinition.allowLegacyNormalRoomFallback) {
                    Debug.LogWarning($"LevelGenerator: Stage {stageDefinition.stageNumber} has no normal rooms and legacy fallback is disabled.");
                }

                if (!stageDefinition.HasSpecialRoomPool(SpecialRoomType.Trap) && !stageDefinition.allowLegacySpecialRoomFallback) {
                    Debug.LogWarning($"LevelGenerator: Stage {stageDefinition.stageNumber} has no trap room pool and legacy fallback is disabled.");
                }

                if (!stageDefinition.HasSpecialRoomPool(SpecialRoomType.Sacrifice) && !stageDefinition.allowLegacySpecialRoomFallback) {
                    Debug.LogWarning($"LevelGenerator: Stage {stageDefinition.stageNumber} has no sacrifice room pool and legacy fallback is disabled.");
                }
            }

            int stageCountToValidate = Mathf.Max(expectedStageCount, highestStageNumber);
            for (int stageNumber = 1; stageNumber <= stageCountToValidate; stageNumber++) {
                if (seenStageNumbers.ContainsKey(stageNumber)) {
                    continue;
                }

                Debug.LogWarning($"LevelGenerator: Missing StageDefinition mapping for stage {stageNumber}.");
            }
        }

        private string BuildActiveStageConfigurationSummary() {
            string requestedStageId = ResolveRequestedStageId();
            string stageLabel = _activeStageDefinition != null
                ? _activeStageDefinition.GetDebugSummary()
                : $"Stage {ResolveRequestedStageNumber()}: no StageDefinition mapped";

            string requestSummary = $"Requested: Stage {ResolveRequestedStageNumber()} / {(string.IsNullOrWhiteSpace(requestedStageId) ? "no-stage-id" : requestedStageId)}";
            string mappingSummary = $"StageDefinition Source: {_activeStageDefinitionSource}";
            string gridSummary = $"Active Grid: {ActiveRoomsHorizontal}x{ActiveRoomsVertical}";
            string normalSummary = $"Normal: {GetRoomCount(_activeNormalRooms)} ({_activeNormalRoomSource})";
            string trapSummary = $"Trap: {GetRoomCount(_activeSpecialRoomPools.TryGetValue(SpecialRoomType.Trap, out Room[] trapRooms) ? trapRooms : null)} ({GetRoomPoolSourceLabel(SpecialRoomType.Trap)}) limit {GetTrapRoomLimit()}";
            string sacrificeSummary = $"Sacrifice: {GetRoomCount(_activeSpecialRoomPools.TryGetValue(SpecialRoomType.Sacrifice, out Room[] sacrificeRooms) ? sacrificeRooms : null)} ({GetRoomPoolSourceLabel(SpecialRoomType.Sacrifice)}) limit {GetSacrificeRoomLimit()}";
            string entranceExitSummary = $"Door Tile Hints: {string.Join(", ", GetEntranceExitTileNameHints())}";
            string safetySummary = $"Entrance Safety: radius {GetEntranceSafetyRadiusTiles()} tiles | clear enemies {ShouldClearEnemiesFromStartRoom()} | clear hazards {ShouldClearImmediateHazardsNearEntrance()}";
            return $"{requestSummary}\n{mappingSummary}\n{gridSummary}\n{stageLabel}\n{normalSummary}\n{trapSummary}\n{sacrificeSummary}\n{entranceExitSummary}\n{safetySummary}";
        }

        private bool TryGetEntranceExitSpawnTile(Room room, string markerLabel, out Tile spawnTile) {
            spawnTile = null;
            if (room == null) {
                Debug.LogError($"LevelGenerator: Failed to place {markerLabel}. Room reference is null.\n{BuildEntranceExitFailureSummary(null, markerLabel)}");
                return false;
            }

            spawnTile = room.GetSuitableEntranceOrExitTile();
            if (spawnTile != null) {
                return true;
            }

            Debug.LogError($"LevelGenerator: Failed to place {markerLabel}. No valid door tile found.\n{BuildEntranceExitFailureSummary(room, markerLabel)}");
            return false;
        }

        private string BuildEntranceExitFailureSummary(Room room, string markerLabel) {
            string stageId = ResolveRequestedStageId();
            string stageSummary = _activeStageDefinition != null
                ? $"{_activeStageDefinition.name} (stage {_activeStageDefinition.stageNumber}, {_activeStageDefinition.stageId})"
                : $"stage {ResolveRequestedStageNumber()} / {(string.IsNullOrWhiteSpace(stageId) ? "no-stage-id" : stageId)}";
            string firstRoomSummary = firstRoom != null ? $"{firstRoom.name} @ {firstRoom.index}" : "null";
            string lastRoomSummary = lastRoom != null ? $"{lastRoom.name} @ {lastRoom.index}" : "null";
            string targetRoomSummary = room != null ? room.GetEntranceExitDebugSummary() : "room=null";
            string doorHints = string.Join(", ", GetEntranceExitTileNameHints());
            return $"Marker={markerLabel}\nStage={stageSummary}\nDoorHints={doorHints}\nFirstRoom={firstRoomSummary}\nLastRoom={lastRoomSummary}\nTargetRoom={targetRoomSummary}";
        }

        private string GetRoomPoolSourceLabel(SpecialRoomType specialRoomType) {
            return _activeSpecialRoomSources.TryGetValue(specialRoomType, out RoomPoolSource roomPoolSource)
                ? roomPoolSource.ToString()
                : RoomPoolSource.Missing.ToString();
        }

        public bool CanSpawnPermanentAccessory(AccessoryType accessoryType) {
            return !PlayerAlreadyHasAccessory(accessoryType) && !_spawnedPermanentAccessories.Contains(accessoryType);
        }

        public bool TryRegisterPermanentAccessorySpawn(AccessoryType accessoryType) {
            if (!CanSpawnPermanentAccessory(accessoryType)) {
                return false;
            }

            return _spawnedPermanentAccessories.Add(accessoryType);
        }

        public void ResetPermanentAccessorySpawnRegistry() {
            _spawnedPermanentAccessories.Clear();
        }

        public void RefreshPermanentAccessorySpawnRegistry() {
            SanitizePermanentAccessoryPickupsInScene();
        }

        private static int GetRoomCount(Room[] roomPool) {
            return roomPool != null ? roomPool.Length : 0;
        }

        private static string GetStageDefinitionLabel(StageDefinition stageDefinition) {
            if (stageDefinition == null) {
                return "null";
            }

            string stageIdLabel = string.IsNullOrWhiteSpace(stageDefinition.stageId) ? "no-stage-id" : stageDefinition.stageId;
            return $"{stageDefinition.name} (stage {stageDefinition.stageNumber}, {stageIdLabel})";
        }

        public bool IsValidEntranceExitTile(Tile tile) {
            if (tile == null) {
                return false;
            }

            string[] tileNameHints = GetEntranceExitTileNameHints();
            if (tileNameHints.Length == 0) {
                return true;
            }

            foreach (string tileNameHint in tileNameHints) {
                if (string.IsNullOrWhiteSpace(tileNameHint)) {
                    continue;
                }

                if (tile.name.IndexOf(tileNameHint, StringComparison.OrdinalIgnoreCase) >= 0) {
                    return true;
                }
            }

            return false;
        }

        private string[] GetEntranceExitTileNameHints() {
            if (_activeStageDefinition != null &&
                _activeStageDefinition.entranceExitTileNameHints != null &&
                _activeStageDefinition.entranceExitTileNameHints.Length > 0) {
                return _activeStageDefinition.entranceExitTileNameHints;
            }

            return new[] { "Dirt" };
        }

        private void ApplyEntranceSafetyRules() {
            if (firstRoom == null || entrance == null) {
                return;
            }

            List<GameObject> threatsToRemove = new List<GameObject>();
            if (ShouldClearEnemiesFromStartRoom()) {
                foreach (Enemy enemy in firstRoom.GetComponentsInChildren<Enemy>(true)) {
                    if (enemy != null) {
                        threatsToRemove.Add(enemy.gameObject);
                    }
                }
            }

            if (ShouldClearImmediateHazardsNearEntrance()) {
                float safeRadius = GetEntranceSafetyRadiusWorld();
                foreach (Transform child in firstRoom.GetComponentsInChildren<Transform>(true)) {
                    if (child == null || child == firstRoom.transform) {
                        continue;
                    }

                    GameObject childObject = child.gameObject;
                    if (!IsImmediateThreatObject(childObject)) {
                        continue;
                    }

                    if (Vector2.Distance(entrance.transform.position, child.position) <= safeRadius) {
                        threatsToRemove.Add(childObject);
                    }
                }
            }

            HashSet<GameObject> uniqueThreats = new HashSet<GameObject>();
            foreach (GameObject threat in threatsToRemove) {
                if (threat == null || !uniqueThreats.Add(threat)) {
                    continue;
                }

                DestroySceneObject(threat);
            }
        }

        private void SanitizePermanentAccessoryPickupsInScene() {
            _spawnedPermanentAccessories.Clear();

            AccessoryPickup[] accessoryPickups = FindObjectsOfType<AccessoryPickup>();
            foreach (AccessoryPickup accessoryPickup in accessoryPickups) {
                if (accessoryPickup == null) {
                    continue;
                }

                if (TryRegisterPermanentAccessorySpawn(accessoryPickup.accessoryType)) {
                    continue;
                }

                DestroySceneObject(accessoryPickup.gameObject);
            }
        }

        private bool PlayerAlreadyHasAccessory(AccessoryType accessoryType) {
            Player player = FindObjectOfType<Player>();
            return player != null && player.Accessories != null && player.Accessories.HasAccessory(accessoryType);
        }

        private bool ShouldClearEnemiesFromStartRoom() {
            return _activeStageDefinition == null || _activeStageDefinition.clearEnemiesFromStartRoom;
        }

        private bool ShouldClearImmediateHazardsNearEntrance() {
            return _activeStageDefinition == null || _activeStageDefinition.clearImmediateHazardsNearEntrance;
        }

        private int GetEntranceSafetyRadiusTiles() {
            return _activeStageDefinition != null ? Mathf.Max(0, _activeStageDefinition.entranceSafetyRadiusTiles) : 4;
        }

        private float GetEntranceSafetyRadiusWorld() {
            return GetEntranceSafetyRadiusTiles() * Tile.Width;
        }

        private static bool IsImmediateThreatObject(GameObject gameObject) {
            if (gameObject == null) {
                return false;
            }

            string objectName = gameObject.name;
            for (int i = 0; i < ImmediateHazardNameHints.Length; i++) {
                if (objectName.IndexOf(ImmediateHazardNameHints[i], StringComparison.OrdinalIgnoreCase) >= 0) {
                    return true;
                }
            }

            return false;
        }

        private static int GetLegacySpecialRoomIndex(SpecialRoomType specialRoomType) {
            switch (specialRoomType) {
                case SpecialRoomType.Trap:
                    return LegacyTrapRoomIndex;
                case SpecialRoomType.Sacrifice:
                    return LegacySacrificeRoomIndex;
                default:
                    return -1;
            }
        }

        private void PickRandomDirection(float downChance) {
            if (Random.value < 1f - downChance) {
                if (Random.value < 0.5f) {
                    _direction = Vector2.right;
                }
                else {
                    _direction = Vector2.left;
                }
            }
            else {
                _direction = Vector2.down;
            }
        }

        /// <summary>
        /// Create an indestructable boundary around the level.
        /// </summary>
        private void CreateLevelBounds() {
            // Straights.
            SpriteRenderer boundsTop = Instantiate(boundsStraight, new Vector3(0, LevelHeight + 48, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsTop.gameObject);
            boundsTop.size = new Vector2(LevelWidth, 64);
            boundsTop.GetComponent<BoxCollider2D>().size = new Vector2(LevelWidth, 48);
            boundsTop.GetComponent<BoxCollider2D>().offset = new Vector2(LevelWidth / 2f, 24);
            boundsTop.transform.localScale = new Vector3(1, -1, 1);
            SpriteRenderer boundsRight = Instantiate(boundsStraight, new Vector3(LevelWidth + 48, 0, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsRight.gameObject);
            boundsRight.size = new Vector2(LevelHeight, 64);
            boundsRight.GetComponent<BoxCollider2D>().size = new Vector2(LevelHeight, 48);
            boundsRight.GetComponent<BoxCollider2D>().offset = new Vector2(LevelHeight / 2f, 24);
            boundsRight.transform.localRotation = Quaternion.Euler(0, 0, 90);
            SpriteRenderer boundsBottom = Instantiate(boundsStraight, new Vector3(0, -48, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsBottom.gameObject);
            boundsBottom.size = new Vector2(LevelWidth, 64);
            boundsBottom.GetComponent<BoxCollider2D>().size = new Vector2(LevelWidth, 48);
            boundsBottom.GetComponent<BoxCollider2D>().offset = new Vector2(LevelWidth / 2f, 24);
            SpriteRenderer boundsLeft = Instantiate(boundsStraight, new Vector3(-48, LevelHeight, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsLeft.gameObject);
            boundsLeft.size = new Vector2(LevelHeight, 64);
            boundsLeft.GetComponent<BoxCollider2D>().size = new Vector2(LevelHeight, 48);
            boundsLeft.GetComponent<BoxCollider2D>().offset = new Vector2(LevelHeight / 2f, 24);
            boundsLeft.transform.localRotation = Quaternion.Euler(0, 0, -90);

            // Corners.
            SpriteRenderer boundsCornerTopLeft = Instantiate(boundsCorner, new Vector3(0, LevelHeight + 48, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsCornerTopLeft.gameObject);
            boundsCornerTopLeft.transform.localRotation = Quaternion.Euler(0, 0, 180);
            SpriteRenderer boundsCornerTopRight = Instantiate(boundsCorner, new Vector3(LevelWidth + 48, LevelHeight, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsCornerTopRight.gameObject);
            boundsCornerTopRight.transform.localRotation = Quaternion.Euler(0, 0, 90);
            SpriteRenderer boundsCornerBottomRight = Instantiate(boundsCorner, new Vector3(LevelWidth, -48, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsCornerBottomRight.gameObject);
            boundsCornerBottomRight.transform.localRotation = Quaternion.Euler(0, 0, 0);
            SpriteRenderer boundsCornerBottomLeft = Instantiate(boundsCorner, new Vector3(-48, 0, 0), Quaternion.identity, _boundsParent);
            TrackGeneratedObject(boundsCornerBottomLeft.gameObject);
            boundsCornerBottomLeft.transform.localRotation = Quaternion.Euler(0, 0, -90);

            // Fill the rest. 2 layers of just corners outside the inner "frame".
        }

        /// <summary>
        /// Just fill the background of the level.
        /// </summary>
        private void CreateBackground() {
            GameObject backgroundPrefab = ResolveBackgroundPrefab();
            string[] backgroundDecalPrefabNames = GetBackgroundDecalPrefabNames();
            float backgroundDecalChance = GetBackgroundDecalChance();

            for (int y = 0; y < RoomHeight * ActiveRoomsVertical * Tile.Height; y += 64) {
                for (int x = 0; x < RoomWidth * ActiveRoomsHorizontal * Tile.Width; x += 64) {
                    GameObject background = Instantiate(
                        backgroundPrefab,
                        new Vector3(x, y, 0),
                        Quaternion.identity,
                        _backgroundParent
                    );
                    TrackGeneratedObject(background);

                    if (backgroundDecalPrefabNames.Length > 0 && Random.value < backgroundDecalChance) {
                        string selectedBackgroundDecalName = backgroundDecalPrefabNames[Random.Range(0, backgroundDecalPrefabNames.Length)];
                        if (_backgroundPrefabs.TryGetValue(selectedBackgroundDecalName, out GameObject backgroundDecalPrefab)) {
                            GameObject backgroundDecal = Instantiate(
                                backgroundDecalPrefab,
                                new Vector3(x + Random.Range(-16, 16), y + Random.Range(-16, 16), 0),
                                Quaternion.identity,
                                _backgroundParent
                            );
                            TrackGeneratedObject(backgroundDecal);
                        }
                        else {
                            Debug.LogWarning($"LevelGenerator: Missing background decal prefab '{selectedBackgroundDecalName}'.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialize all the tiles in the level.
        /// </summary>
        private void InitializeTiles() {
            // Find all tiles in the level.
            Tile[] tempTiles = FindObjectsOfType<Tile>();
            foreach (Tile tile in tempTiles) {
                // Check if we should remove the tile.
                if (tile.spawnProbability <= Random.Range(0, 100)) {
                    Destroy(tile.gameObject);
                    continue;
                }

                // Otherwise initialize the tile.
                int x = (int)tile.transform.position.x / Tile.Width;
                int y = (int)tile.transform.position.y / Tile.Height;
                tile.InitializeTile(x, y);
                tile.debug = debug;
            }
        }

        /// <summary>
        /// Loop through and setup all the tiles in the level.
        /// This gives the correct sprite and decorations etc.
        /// </summary>
        private void SetupTiles() {
            for (int x = 0; x < Tiles.GetLength(0); x++) {
                for (int y = 0; y < Tiles.GetLength(1); y++) {
                    // No tile.
                    if (Tiles[x, y] == null) {
                        continue;
                    }

                    Tiles[x, y].SetupTile();
                }
            }
        }

        /// <summary>
        /// Remove tiles from the level.
        /// </summary>
        /// <param name="tilesToRemove"></param>
        public void RemoveTiles(Tile[] tilesToRemove) {
            // Find the bounds of the tiles to remove while we remove the specified tiles.
            int minX = int.MaxValue;
            int maxX = -1;
            int minY = int.MaxValue;
            int maxY = -1;
            foreach (Tile tile in tilesToRemove) {
                if (tile.x < minX) {
                    minX = tile.x;
                }

                if (tile.x > maxX) {
                    maxX = tile.x;
                }

                if (tile.y < minY) {
                    minY = tile.y;
                }

                if (tile.y > maxY) {
                    maxY = tile.y;
                }

                // Remove the specified tile.
                tile.Remove();
            }

            // Expand the bounds by 1...
            minX--;
            maxX++;
            minY--;
            maxY++;

            // But ensure we stay within the level bounds.
            if (minX < 0) {
                minX = 0;
            }

            if (maxX >= Tiles.GetLength(0)) {
                maxX = Tiles.GetLength(0) - 1;
            }

            if (minY < 0) {
                minY = 0;
            }

            if (maxY >= Tiles.GetLength(1)) {
                maxY = Tiles.GetLength(1) - 1;
            }

            // Setup the tiles surrounding the tiles we just removed using the bounds we've just founds, so that the
            // affected tiles get the correct sprites and decorations now that their neighbor tiles are gone.
            for (int x = minX; x <= maxX; x++) {
                for (int y = minY; y <= maxY; y++) {
                    // No tile.
                    if (Tiles[x, y] == null) {
                        continue;
                    }

                    Tiles[x, y].SetupTile();
                }
            }
        }

        private void AllocateRuntimeArrays() {
            Rooms = new Room[ActiveRoomsHorizontal, ActiveRoomsVertical];
            Tiles = new Tile[ActiveRoomsHorizontal * RoomWidth, ActiveRoomsVertical * RoomHeight];
        }

        private void TrackGeneratedObject(GameObject generatedObject) {
            if (generatedObject == null) {
                return;
            }

            _generatedLevelObjects.Add(generatedObject);
        }

        private int GetTrackedGeneratedObjectCount() {
            int trackedCount = 0;
            for (int i = 0; i < _generatedLevelObjects.Count; i++) {
                if (_generatedLevelObjects[i] != null) {
                    trackedCount++;
                }
            }

            return trackedCount;
        }

        private static void DestroyTrackedObject(GameObject generatedObject) {
            if (generatedObject == null) {
                return;
            }

            if (Application.isPlaying) {
                Destroy(generatedObject);
            }
            else {
                DestroyImmediate(generatedObject);
            }
        }

        private static void DestroySceneObject(GameObject sceneObject) {
            if (sceneObject == null) {
                return;
            }

            if (Application.isPlaying) {
                Destroy(sceneObject);
            }
            else {
                DestroyImmediate(sceneObject);
            }
        }

        private GameObject ResolveBackgroundPrefab() {
            string backgroundPrefabName = _activeStageDefinition != null && !string.IsNullOrWhiteSpace(_activeStageDefinition.backgroundPrefabName)
                ? _activeStageDefinition.backgroundPrefabName
                : "Background";

            if (_backgroundPrefabs.TryGetValue(backgroundPrefabName, out GameObject backgroundPrefab)) {
                return backgroundPrefab;
            }

            Debug.LogWarning($"LevelGenerator: Missing background prefab '{backgroundPrefabName}'. Falling back to Background.");
            return _backgroundPrefabs["Background"];
        }

        private string[] GetBackgroundDecalPrefabNames() {
            if (_activeStageDefinition != null &&
                _activeStageDefinition.backgroundDecalPrefabNames != null &&
                _activeStageDefinition.backgroundDecalPrefabNames.Length > 0) {
                return _activeStageDefinition.backgroundDecalPrefabNames;
            }

            return new[] { "BackgroundDecal", "BackgroundDecal_2" };
        }

        private float GetBackgroundDecalChance() {
            return _activeStageDefinition != null ? _activeStageDefinition.backgroundDecalChance : 0.1f;
        }

    }

}


