using System;
using System.Collections;
using TwiiK.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Spelunky {

    public class GameManager : Singleton<GameManager> {

        private const int MaxProceduralBuildAttempts = 3;
        private const float TemporaryStageMessageDurationSeconds = 1.25f;
        private const string FinalEscapeTimeoutCause = "FinalEscapeTimeout";
        private static readonly Color FinalEscapeTimerColor = new Color(1f, 0.9f, 0.35f, 1f);
        private static readonly Color FinalEscapeUrgentTimerColor = new Color(1f, 0.3f, 0.3f, 1f);

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

        private Text _finalEscapeTimerText;
        private Coroutine _stageMessageCoroutine;

        public override void Awake() {
            base.Awake();
            RunManager.EnsureInstance();
            CreateSubManagers();
        }

        private void Start() {
            InitializeLevel();
            RunManager.Instance.RegisterGameScene();
            RefreshStageMusicForCurrentRun();
            ConfigureStageClimaxForCurrentRun();
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

            ResetStageClimaxRuntime();

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
                RefreshStageMusicForCurrentRun();
                ConfigureStageClimaxForCurrentRun();
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
            if (hud == null) {
                return;
            }

            if (hud.AccessoriesContainer != null) {
                RunState currentRun = RunManager.Instance != null ? RunManager.Instance.CurrentRun : null;
                bool shouldClearAccessories = currentRun == null || currentRun.accessoryIds == null || currentRun.accessoryIds.Count == 0;
                if (shouldClearAccessories) {
                    ClearChildren(hud.AccessoriesContainer);
                }
            }

            RefreshFinalEscapeHud();
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
                return;
            }

            CameraFollow[] lingeringCameras = FindObjectsOfType<CameraFollow>();
            for (int i = 0; i < lingeringCameras.Length; i++) {
                CameraFollow lingeringCamera = lingeringCameras[i];
                if (lingeringCamera == null) {
                    continue;
                }

                DestroyRuntimeObject(lingeringCamera.gameObject);
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

            UpdateFinalStageClimax();
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

            ResetStageClimaxRuntime();
            GameOverUI.ShowResult(CreateDeathResultModel(score));
        }

        public void HandlePlayerEnteredExit(Player player, Exit exitDoor) {
            if (IsGameOver || IsTransitioning) {
                return;
            }

            if (!CanPlayerEnterExit(player, exitDoor)) {
                HandleLockedExitAttempt(player, exitDoor);
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

        public bool CanPlayerEnterExit(Player player, Exit exitDoor) {
            RunManager runManager = RunManager.Instance;
            return runManager == null || runManager.CanUseExit();
        }

        public void HandleLockedExitAttempt(Player player, Exit exitDoor) {
            if (RunManager.Instance == null || !RunManager.Instance.IsFinalEscapePending) {
                return;
            }

            ShowTemporaryStageMessage("EXIT SEALED", "STEAL THE IDOL");
        }

        private void ActivateFinalEscapeFromGoldIdol(Player player) {
            RunManager runManager = RunManager.Instance;
            if (runManager == null || !runManager.TryActivateFinalEscape("gold-idol")) {
                RefreshFinalEscapeHud();
                return;
            }

            float timeRemaining = runManager.GetFinalEscapeTimeRemaining();
            Debug.Log($"GameManager: Gold Idol recovered. Escape timer started with {timeRemaining:0.0}s.");
            ShowTemporaryStageMessage("IDOL RECOVERED", $"ESCAPE IN {Mathf.CeilToInt(timeRemaining)}s");
            RefreshFinalEscapeHud();
        }

        private void ConfigureStageClimaxForCurrentRun() {
            RefreshFinalEscapeHud();

            RunManager runManager = RunManager.Instance;
            if (runManager == null || !runManager.IsCurrentStageFinal) {
                return;
            }

            if (runManager.IsFinalEscapePending) {
                ShowTemporaryStageMessage("STAGE 4", "STEAL THE IDOL");
                return;
            }
        }

        private void RefreshStageMusicForCurrentRun() {
            if (AudioManager.Instance == null || RunManager.Instance == null || RunManager.Instance.CurrentRun == null) {
                return;
            }

            AudioManager.Instance.PlayStageMusic(RunManager.Instance.CurrentRun.currentStageIndex);
        }

        private void UpdateFinalStageClimax() {
            TryActivateFinalEscapeFromHeldGoldIdol();
            RefreshFinalEscapeHud();

            RunManager runManager = RunManager.Instance;
            if (runManager == null || !runManager.IsFinalEscapeActive || !runManager.HasFinalEscapeExpired()) {
                return;
            }

            if (ActivePlayer == null || ActivePlayer.Health == null || ActivePlayer.Health.CurrentHealth <= 0) {
                return;
            }

            Debug.Log("GameManager: Final escape timer expired.");
            RunManager.Instance.SetPendingDeathCause(FinalEscapeTimeoutCause);
            using (DebugDamageContext.Use(FinalEscapeTimeoutCause)) {
                ActivePlayer.Health.TakeDamage(ActivePlayer.Health.CurrentHealth);
            }
        }

        private void TryActivateFinalEscapeFromHeldGoldIdol() {
            RunManager runManager = RunManager.Instance;
            if (runManager == null || !runManager.IsFinalEscapePending || ActivePlayer == null || ActivePlayer.Holding == null) {
                return;
            }

            IHoldable heldItem = ActivePlayer.Holding.HeldItem;
            if (!IsGoldIdol(heldItem)) {
                return;
            }

            ActivateFinalEscapeFromGoldIdol(ActivePlayer);
        }

        private void ResetStageClimaxRuntime() {
            HideTemporaryStageMessage();
            RefreshFinalEscapeHud();
        }

        private void RefreshFinalEscapeHud() {
            Text timerText = EnsureFinalEscapeTimerText();
            if (timerText == null) {
                return;
            }

            RunManager runManager = RunManager.Instance;
            bool shouldShow = !IsGameOver && runManager != null && runManager.IsFinalEscapeActive;
            timerText.gameObject.SetActive(shouldShow);
            if (!shouldShow) {
                timerText.text = string.Empty;
                return;
            }

            float remaining = runManager.GetFinalEscapeTimeRemaining();
            timerText.text = $"ESCAPE {remaining:0.0}s";
            timerText.color = remaining <= 10f ? FinalEscapeUrgentTimerColor : FinalEscapeTimerColor;
        }

        private Text EnsureFinalEscapeTimerText() {
            if (_finalEscapeTimerText != null) {
                return _finalEscapeTimerText;
            }

            PlayerHUDReferences hud = ResolvePlayerHud();
            Transform parent = hud != null && hud.CanvasRoot != null ? hud.CanvasRoot.transform : null;
            if (parent == null) {
                return null;
            }

            Transform existing = parent.Find("FinalEscapeTimerText");
            if (existing != null) {
                _finalEscapeTimerText = existing.GetComponent<Text>();
                return _finalEscapeTimerText;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject timerTextObject = new GameObject("FinalEscapeTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timerTextObject.transform.SetParent(parent, false);

            RectTransform rect = timerTextObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(240f, 22f);

            Text timerText = timerTextObject.GetComponent<Text>();
            timerText.font = font;
            timerText.fontSize = 14;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.UpperCenter;
            timerText.raycastTarget = false;
            timerText.text = string.Empty;
            timerText.color = FinalEscapeTimerColor;

            _finalEscapeTimerText = timerText;
            timerText.gameObject.SetActive(false);
            return _finalEscapeTimerText;
        }

        private static bool IsGoldIdol(IHoldable holdable) {
            if (holdable == null || holdable.transform == null) {
                return false;
            }

            return holdable.transform.name.IndexOf("GoldIdol", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ShowTemporaryStageMessage(string title, string detail) {
            if (!isActiveAndEnabled) {
                return;
            }

            HideTemporaryStageMessage();
            _stageMessageCoroutine = StartCoroutine(ShowTemporaryStageMessageRoutine(title, detail));
        }

        private IEnumerator ShowTemporaryStageMessageRoutine(string title, string detail) {
            UIManager uiManager = UIManager.EnsureInstance();
            uiManager?.ShowTransition(title, detail);
            yield return new WaitForSeconds(TemporaryStageMessageDurationSeconds);

            if (!IsGameOver && UIManager.Instance != null && UIManager.Instance.CurrentState == UIManager.UIFlowState.Transition) {
                UIManager.Instance.HideTransition();
            }

            _stageMessageCoroutine = null;
        }

        private void HideTemporaryStageMessage() {
            if (_stageMessageCoroutine != null) {
                StopCoroutine(_stageMessageCoroutine);
                _stageMessageCoroutine = null;
            }

            if (!IsGameOver && UIManager.Instance != null && UIManager.Instance.CurrentState == UIManager.UIFlowState.Transition) {
                UIManager.Instance.HideTransition();
            }
        }

        private GameOverUI.ResultViewModel CreateDeathResultModel(int score) {
            string deathCause = GetDeathCauseDisplayText();
            return new GameOverUI.ResultViewModel {
                Preset = GameOverUI.ResultPreset.GameOver,
                Title = "RIP",
                ValueLabel = "사인",
                ValueText = $"{deathCause}\n점수 {score}",
                PrimaryActionLabel = "RESTART",
                PrimaryAction = RestartCurrentRun
            };
        }

        private string GetDeathCauseDisplayText() {
            string deathCause = RunManager.Instance?.LastCompletedResult?.finalDeathCause;
            if (string.IsNullOrWhiteSpace(deathCause)) {
                return "끝내 원인은 밝혀지지 않았다";
            }

            if (deathCause.StartsWith("EnemyContact:", StringComparison.Ordinal)) {
                return GetEnemyContactDeathText(deathCause.Substring("EnemyContact:".Length));
            }

            if (deathCause.StartsWith("EnemyDamage:", StringComparison.Ordinal)) {
                return GetEnemyDamageDeathText(deathCause.Substring("EnemyDamage:".Length));
            }

            if (deathCause.StartsWith("DamageArea:", StringComparison.Ordinal)) {
                return GetDamageAreaDeathText(deathCause.Substring("DamageArea:".Length));
            }

            if (deathCause.StartsWith("Explosion:", StringComparison.Ordinal)) {
                return GetExplosionDeathText(deathCause.Substring("Explosion:".Length));
            }

            switch (deathCause) {
                case "Crush":
                    return "무너지는 돌더미 아래 깔려 숨을 거두었다";
                case FinalEscapeTimeoutCause:
                    return "탈출이 늦어 끝내 심연에 삼켜졌다";
            }

            string normalizedCause = deathCause.Replace('_', ' ').Trim();
            return $"{normalizedCause} 끝에 생을 마쳤다";
        }

        private static string GetEnemyContactDeathText(string sourceName) {
            string normalizedName = NormalizeDeathSourceName(sourceName);
            switch (normalizedName) {
                case "Bat":
                    return "박쥐의 그림자 같은 습격에 정신을 차릴 틈도 없이 쓰러졌다";
                case "Caveman":
                    return "원시인의 거친 난동에 휘말려 끝내 쓰러졌다";
                case "Snake":
                    return "뱀의 날카로운 일격에 발목이 잡혀 생을 마쳤다";
                case "Spider":
                    return "거미의 집요한 습격을 벗어나지 못하고 숨을 거두었다";
                default:
                    string enemyName = GetDeathSourceDisplayName(normalizedName);
                    return string.IsNullOrWhiteSpace(enemyName)
                        ? "이름 모를 적의 습격에 쓰러졌다"
                        : $"{enemyName}의 습격 앞에 쓰러졌다";
            }
        }

        private static string GetEnemyDamageDeathText(string sourceName) {
            string normalizedName = NormalizeDeathSourceName(sourceName);
            switch (normalizedName) {
                case "Bat":
                    return "박쥐가 남긴 상처가 끝내 발목을 붙잡았다";
                case "Caveman":
                    return "원시인이 휘두른 거친 힘을 버티지 못하고 무너졌다";
                case "Snake":
                    return "뱀의 기습적인 공격에 정신을 잃고 말았다";
                case "Spider":
                    return "거미의 맹독 같은 습격 앞에 끝내 쓰러졌다";
                default:
                    string enemyName = GetDeathSourceDisplayName(normalizedName);
                    return string.IsNullOrWhiteSpace(enemyName)
                        ? "이름 모를 적의 공격에 목숨을 잃었다"
                        : $"{enemyName}의 공격을 버티지 못하고 쓰러졌다";
            }
        }

        private static string GetDamageAreaDeathText(string sourceName) {
            string normalizedName = NormalizeDeathSourceName(sourceName);
            switch (normalizedName) {
                case "Spikes":
                case "Spike":
                    return "가시 함정에 몸이 꿰뚫려 그대로 생을 마쳤다";
                case "ArrowTrap":
                    return "화살 함정이 쏜 살을 피하지 못하고 쓰러졌다";
                case "Arrow":
                    return "날아든 화살이 깊이 박혀 끝내 숨이 멎었다";
                default:
                    string displayName = GetDeathSourceDisplayName(normalizedName);
                    return string.IsNullOrWhiteSpace(displayName)
                        ? "정체불명의 함정에 목숨을 잃었다"
                        : $"{displayName}에 휩쓸려 생을 마쳤다";
            }
        }

        private static string GetExplosionDeathText(string sourceName) {
            string normalizedName = NormalizeDeathSourceName(sourceName);
            switch (normalizedName) {
                case "Bomb":
                    return "폭탄의 불길한 섬광과 함께 흔적도 없이 날아갔다";
                case "ArrowTrap":
                    return "화살 함정 주변의 폭발에 휘말려 쓰러졌다";
                default:
                    string displayName = GetDeathSourceDisplayName(normalizedName);
                    return string.IsNullOrWhiteSpace(displayName)
                        ? "거센 폭발에 휘말려 쓰러졌다"
                        : $"{displayName} 폭발에 휘말려 쓰러졌다";
            }
        }

        private static string GetDeathSourceDisplayName(string sourceName) {
            string normalizedName = NormalizeDeathSourceName(sourceName);

            switch (normalizedName) {
                case "Bat":
                    return "박쥐";
                case "Caveman":
                    return "원시인";
                case "Snake":
                    return "뱀";
                case "Spider":
                    return "거미";
                case "Spikes":
                case "Spike":
                    return "가시";
                case "ArrowTrap":
                    return "화살 함정";
                case "Arrow":
                    return "화살";
                case "Bomb":
                    return "폭탄";
                case "GoldIdol":
                    return "황금 우상";
                default:
                    return normalizedName;
            }
        }

        private static string NormalizeDeathSourceName(string sourceName) {
            string normalizedName = sourceName
                .Replace("(Clone)", string.Empty)
                .Trim();

            int coordinateSuffixIndex = normalizedName.IndexOf('[');
            if (coordinateSuffixIndex >= 0) {
                normalizedName = normalizedName.Substring(0, coordinateSuffixIndex).TrimEnd();
            }

            return normalizedName;
        }

        private void RestartCurrentRun() {
            if (RunManager.Instance != null) {
                RunManager.Instance.RestartRun(gameObject.scene.name);
                return;
            }

            SceneManager.LoadScene(gameObject.scene.name);
        }

    }

}
