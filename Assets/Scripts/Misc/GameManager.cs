using System;
using TwiiK.Utility;
using UnityEngine;

namespace Spelunky {

    public class GameManager : Singleton<GameManager> {

        private const int MaxProceduralBuildAttempts = 3;

        [Header("Player")]
        public Player player;
        public CameraFollow playerCamera;

        [Header("Level")]
        [Tooltip("Reference to the LevelGenerator in the scene")]
        public LevelGenerator levelGenerator;

        [Tooltip("When true, skips procedural generation and scans the scene for existing entities")]
        public bool useExistingSceneContent;

        // Sub-managers for the centralized game loop
        public PlatformManager PlatformManager { get; private set; }
        public EntityManager EntityManager { get; private set; }
        public TimerManager TimerManager { get; private set; }

        public bool IsGameOver { get; private set; }
        public bool IsTransitioning { get; private set; }
        public Player ActivePlayer { get; private set; }

        public override void Awake() {
            base.Awake();
            RunManager.EnsureInstance();
            CreateSubManagers();
        }

        private void Start() {
            InitializeLevel();
            RunManager.Instance.RegisterGameScene();
        }

        private void CreateSubManagers() {
            // Create child GameObjects for each sub-manager
            GameObject platformManagerObj = new GameObject("PlatformManager");
            platformManagerObj.transform.SetParent(transform);
            PlatformManager = platformManagerObj.AddComponent<PlatformManager>();

            GameObject entityManagerObj = new GameObject("EntityManager");
            entityManagerObj.transform.SetParent(transform);
            EntityManager = entityManagerObj.AddComponent<EntityManager>();

            GameObject timerManagerObj = new GameObject("TimerManager");
            timerManagerObj.transform.SetParent(transform);
            TimerManager = timerManagerObj.AddComponent<TimerManager>();
        }

        private void InitializeLevel() {
            if (levelGenerator == null) {
                Debug.LogError("GameManager: No LevelGenerator assigned!");
                return;
            }

            PrepareLevelForCurrentRun();

            if (useExistingSceneContent) {
                // Testing mode: scan for existing entities and register them
                ScanAndRegisterExistingEntities();
            } else {
                // Normal mode: procedurally generate the level
                if (!TryBuildProceduralLevel()) {
                    Debug.LogError("GameManager: Failed to build a procedural level with a valid entrance/exit.");
                    return;
                }
            }

            if (useExistingSceneContent) {
                // Always setup the level (tiles, bounds, background)
                levelGenerator.SetupLevel();
            }

            RefreshHudStateForCurrentRun();

            // Spawn the player at the entrance (unless one already exists in testing mode)
            Player existingPlayer = FindObjectOfType<Player>();
            if (existingPlayer != null) {
                ActivePlayer = existingPlayer;
                // Player already exists in the scene, just set up the camera
                CameraFollow existingCam = FindObjectOfType<CameraFollow>();
                if (existingCam != null) {
                    existingCam.Initialize(existingPlayer);
                    existingPlayer.cam = existingCam;
                } else {
                    // Spawn camera for existing player
                    CameraFollow camInstance = Instantiate(playerCamera, existingPlayer.transform.position, Quaternion.identity);
                    camInstance.Initialize(existingPlayer);
                    existingPlayer.cam = camInstance;
                }

                RunManager.Instance.BindPlayer(existingPlayer);
            } else {
                // Spawn a new player at the entrance
                SpawnPlayer(levelGenerator.entrance.transform.position);
            }
        }

        private void PrepareLevelForCurrentRun() {
            RunStageLoadRequest stageRequest = RunManager.Instance?.GetCurrentStageLoadRequest(gameObject.scene.name);
            if (stageRequest == null) {
                return;
            }

            PrepareLevelForStage(stageRequest);
        }

        public bool TryStartStageInPlace(RunStageLoadRequest stageRequest) {
            if (stageRequest == null) {
                return false;
            }

            if (useExistingSceneContent) {
                Debug.Log($"GameManager: In-place stage transition is disabled for existing-scene-content mode. Falling back to scene reload for {stageRequest}.");
                return false;
            }

            if (levelGenerator == null || player == null || playerCamera == null) {
                Debug.LogWarning($"GameManager: Missing stage transition dependencies. Falling back to scene reload for {stageRequest}.");
                return false;
            }

            if (!PrepareLevelForStage(stageRequest)) {
                return false;
            }

            IsGameOver = false;
            IsTransitioning = true;

            try {
                Debug.Log($"GameManager: Starting in-place stage transition for {stageRequest}.");

                TimerManager?.CancelAllTimers();

                DestroyActivePlayerAndCamera();
                levelGenerator.ClearGeneratedLevel();
                DestroyLooseRuntimeObjects();
                ResetSubManagers();

                if (!TryBuildProceduralLevel()) {
                    Debug.LogWarning($"GameManager: Failed to build procedural content for {stageRequest}. Falling back to scene reload.");
                    return false;
                }

                RefreshHudStateForCurrentRun();
                SpawnPlayer(levelGenerator.entrance.transform.position);

                RunManager.Instance?.RegisterGameScene();
                IsGameOver = false;
                return true;
            }
            catch (Exception exception) {
                Debug.LogWarning($"GameManager: In-place stage transition failed for {stageRequest}. Falling back to scene reload. {exception.Message}");
                return false;
            }
            finally {
                IsTransitioning = false;
            }
        }

        private bool PrepareLevelForStage(RunStageLoadRequest stageRequest) {
            bool stageResolved = levelGenerator.SetStageIndex(stageRequest.stageIndex);
            if (!stageResolved) {
                Debug.LogWarning($"GameManager: Failed to resolve stage configuration for {stageRequest}. Falling back to LevelGenerator defaults.");
                return false;
            }

            Debug.Log($"GameManager: Prepared level generator for {stageRequest}.");
            return true;
        }

        private void RefreshHudStateForCurrentRun() {
            PlayerHUDReferences hud = ResolvePlayerHud();
            if (hud == null || hud.AccessoriesContainer == null) {
                return;
            }

            RunState currentRun = RunManager.Instance != null ? RunManager.Instance.CurrentRun : null;
            bool shouldClearAccessories = currentRun == null || currentRun.accessoryIds == null || currentRun.accessoryIds.Count == 0;
            if (!shouldClearAccessories) {
                return;
            }

            ClearChildren(hud.AccessoriesContainer);
        }

        private bool TryBuildProceduralLevel() {
            for (int attempt = 1; attempt <= MaxProceduralBuildAttempts; attempt++) {
                try {
                    levelGenerator.GenerateLevel();
                    levelGenerator.SetupLevel();
                    levelGenerator.PlaceEntranceAndExit();
                    return true;
                }
                catch (NullReferenceException exception) {
                    Debug.LogWarning($"GameManager: Procedural build attempt {attempt}/{MaxProceduralBuildAttempts} failed while placing entrance/exit. {exception.Message}");
                    levelGenerator.ClearGeneratedLevel();
                }
            }

            return false;
        }

        private void DestroyActivePlayerAndCamera() {
            CameraFollow activeCamera = ActivePlayer != null ? ActivePlayer.cam : null;

            if (ActivePlayer != null) {
                DestroyRuntimeObject(ActivePlayer.gameObject);
                ActivePlayer = null;
            }

            if (activeCamera != null) {
                DestroyRuntimeObject(activeCamera.gameObject);
            }
        }

        private void DestroyLooseRuntimeObjects() {
            DestroyComponentsOfType<PhysicsBody>();
            DestroyComponentsOfType<Rope>();
            DestroyComponentsOfType<Explosion>();
            DestroyComponentsOfType<AccessoryPickup>();
            DestroyComponentsOfType<InventoryPickup>();
        }

        private void ResetSubManagers() {
            DestroySubManager(PlatformManager);
            DestroySubManager(EntityManager);
            DestroySubManager(TimerManager);

            PlatformManager = null;
            EntityManager = null;
            TimerManager = null;

            CreateSubManagers();
        }

        private static void DestroySubManager(Component manager) {
            if (manager == null) {
                return;
            }

            GameObject managerObject = manager.gameObject;
            if (managerObject == null) {
                return;
            }

            managerObject.SetActive(false);
            UnityEngine.Object.Destroy(managerObject);
        }

        private static PlayerHUDReferences ResolvePlayerHud() {
            UIManager uiManager = UIManager.EnsureInstance();
            if (uiManager != null && uiManager.PlayerHUD != null) {
                return uiManager.PlayerHUD;
            }

            return FindObjectOfType<PlayerHUDReferences>();
        }

        private static void ClearChildren(Transform parent) {
            if (parent == null) {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--) {
                Transform child = parent.GetChild(i);
                if (child != null) {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }

        private void DestroyComponentsOfType<T>() where T : Component {
            T[] components = FindObjectsOfType<T>();
            for (int i = 0; i < components.Length; i++) {
                T component = components[i];
                if (component == null) {
                    continue;
                }

                DestroyRuntimeObject(component.gameObject);
            }
        }

        private void DestroyRuntimeObject(GameObject runtimeObject) {
            if (runtimeObject == null) {
                return;
            }

            runtimeObject.SetActive(false);
            Destroy(runtimeObject);
        }

        private void ScanAndRegisterExistingEntities() {
            // Find and register all MovingPlatforms
            MovingPlatform[] platforms = FindObjectsOfType<MovingPlatform>();
            foreach (MovingPlatform platform in platforms) {
                PlatformManager.Register(platform);
            }
            Debug.Log($"GameManager: Registered {platforms.Length} existing MovingPlatforms");

            // Find and register all ITickable entities
            // Note: MonoBehaviours that implement ITickable
            MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
            int entityCount = 0;
            foreach (MonoBehaviour behaviour in allBehaviours) {
                if (behaviour is ITickable tickable && !(behaviour is MovingPlatform)) {
                    EntityManager.Register(tickable);
                    entityCount++;
                }
            }
            Debug.Log($"GameManager: Registered {entityCount} existing entities");
        }

        private void Update() {
            if (IsGameOver || IsTransitioning) {
                return;
            }

            // ===== 1. INPUT PHASE =====
            // Read input, update directional input
            EntityManager.EarlyTick();

            // ===== 2. PRE-PHYSICS PHASE =====
            // MovingPlatforms set externalDelta on riders
            PlatformManager.Tick();

            // ===== 3. PHYSICS PHASE =====
            // State machine updates, velocity calculations, entity movement
            EntityManager.Tick();

            // ===== 4. POST-PHYSICS PHASE =====
            // Player.HandleEnemyOverlaps(), State.ChangePlayerVelocityAfterMove()
            EntityManager.LateTick();

            // ===== 5. TIMER PHASE =====
            // Process all active timers
            TimerManager.Tick();
        }

        public void HandlePlayerDeath(Player player) {
            Debug.Log("GameManager: Player has died, handling game over...");
            
            if (IsGameOver || IsTransitioning) {
                return;
            }

            IsGameOver = true;
            RunManager.Instance?.RecordPlayerDeath(player);

            int score = 0;
            if (player != null && player.Inventory != null) {
                score = player.Inventory.goldAmount;
            }
            
            Debug.Log($"GameManager: Player score at death: {score}");

            GameOverUI.ShowGameOver(score);
        }

        public void HandlePlayerEnteredExit(Player player, Exit exitDoor) {
            if (IsGameOver || IsTransitioning) {
                return;
            }

            IsTransitioning = true;

            string exitName = exitDoor != null ? exitDoor.name : "unknown";
            Debug.Log($"GameManager: Player entered exit '{exitName}'. Requesting next stage flow.");

            RunManager.Instance.RequestNextStage(gameObject.scene.name);
        }

        public void SpawnPlayer(Vector3 position) {
            // Bump us half a tile to the right so we're in the center of the entrance.
            Player playerInstance = Instantiate(player, position + new Vector3(8, 0, 0), Quaternion.identity);
            // Bump the camera half a tile up as well so it's in the correct spot right away.
            CameraFollow camInstance = Instantiate(playerCamera, position + new Vector3(8, 8, 0), Quaternion.identity);
            camInstance.Initialize(playerInstance);
            playerInstance.cam = camInstance;
            ActivePlayer = playerInstance;
            RunManager.Instance.BindPlayer(playerInstance);
        }

    }

}
