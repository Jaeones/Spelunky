using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spelunky {

    /// <summary>
    /// Owns run-scoped state and receives stage advance requests.
    /// For now it preserves current gameplay by re-entering the Game scene.
    /// </summary>
    public class RunManager : MonoBehaviour {

        public static RunManager Instance { get; private set; }
        public const int DefaultStageCount = RunState.DefaultTotalStageCount;

        public RunState CurrentRun { get; private set; }
        public RunResult CurrentResult { get; private set; }
        public RunResult LastCompletedResult { get; private set; }
        public Player ActivePlayer { get; private set; }
        public RunStageLoadRequest PendingStageLoadRequest { get; private set; }
        public string LastRunResultLogPath { get; private set; }

        private readonly List<RunResult> _completedResults = new List<RunResult>();
        private StageRunResult _activeStageResult;
        private string _pendingDeathCause;

        public static RunManager EnsureInstance() {
            if (Instance != null) {
                return Instance;
            }

            GameObject runManagerObject = new GameObject("RunManager");
            return runManagerObject.AddComponent<RunManager>();
        }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureRun();
        }

        private void Update() {
            if (CurrentRun == null || _activeStageResult == null) {
                return;
            }

            float deltaTime = Time.deltaTime;
            CurrentRun.elapsedTime += deltaTime;
            CurrentRun.currentStageElapsedTime += deltaTime;
        }

        public void EnsureRun() {
            if (CurrentRun != null) {
                return;
            }

            StartNewRun();
        }

        public void StartFreshRun() {
            ResetRuntimeReferences();
            StartNewRun();
        }

        public void RegisterGameScene() {
            EnsureRun();
            RunStageLoadRequest stageRequest = GetCurrentStageLoadRequest(SceneManager.GetActiveScene().name);
            ApplyStageLoadRequest(stageRequest);
            EnterCurrentStage(stageRequest.sceneName);
            PendingStageLoadRequest = null;
            Debug.Log($"RunManager: Game scene ready for stage {CurrentRun.currentStageIndex}.");
        }

        public void RequestNextStage(string currentSceneName) {
            EnsureRun();
            if (CurrentRun.progressState != RunProgressState.Active) {
                Debug.LogWarning($"RunManager: Ignoring next stage request while run state is {CurrentRun.progressState}.");
                return;
            }

            CapturePlayerState(ActivePlayer);
            BeginStageTransition();
            CompleteActiveStage("cleared");

            RunStageLoadRequest nextStageRequest = CreateNextStageLoadRequest(currentSceneName);
            if (nextStageRequest == null) {
                CompleteRunClear(currentSceneName);
                return;
            }

            LoadStage(nextStageRequest);
        }

        public void BindPlayer(Player player) {
            EnsureRun();
            ActivePlayer = player;

            if (player == null) {
                return;
            }

            ApplyRunStateToPlayer(player);
            CapturePlayerState(player);
        }

        public void CapturePlayerState(Player player) {
            EnsureRun();

            if (player == null) {
                return;
            }

            ActivePlayer = player;
            CurrentRun.health = player.Health.CurrentHealth;
            CurrentRun.maxHealth = player.Health.maxHealth;
            CurrentRun.bombs = player.Inventory.numberOfBombs;
            CurrentRun.ropes = player.Inventory.numberOfRopes;
            CurrentRun.gold = player.Inventory.goldAmount;
            CurrentRun.accessoryIds = player.Accessories.GetAccessoryIds();
        }

        public void ApplyRunStateToPlayer(Player player) {
            EnsureRun();

            if (player == null) {
                return;
            }

            player.Health.SetMaxHealth(CurrentRun.maxHealth);
            player.Health.SetCurrentHealth(CurrentRun.health);
            player.Inventory.SetBombs(CurrentRun.bombs);
            player.Inventory.SetRopes(CurrentRun.ropes);
            player.Inventory.SetGold(CurrentRun.gold);
            player.Accessories.SetAccessories(CurrentRun.accessoryIds, false);
        }

        public void ForceSetResources(int health, int bombs, int ropes) {
            EnsureRun();

            CurrentRun.health = Mathf.Clamp(health, 1, Mathf.Max(1, CurrentRun.maxHealth));
            CurrentRun.bombs = Mathf.Max(0, bombs);
            CurrentRun.ropes = Mathf.Max(0, ropes);

            if (ActivePlayer != null) {
                ApplyRunStateToPlayer(ActivePlayer);
            }

            Debug.Log($"RunManager: Forced resources to HP {CurrentRun.health}/{CurrentRun.maxHealth}, Bombs {CurrentRun.bombs}, Ropes {CurrentRun.ropes}.");
        }

        public void DebugWarpToStage(int stageIndex, string sceneName = null) {
            EnsureRun();

            string targetSceneName = string.IsNullOrWhiteSpace(sceneName) ? SceneManager.GetActiveScene().name : sceneName;
            CapturePlayerState(ActivePlayer);
            BeginStageTransition();
            CompleteActiveStage("debug-warp");

            RunStageLoadRequest debugRequest = CreateStageLoadRequest(stageIndex, targetSceneName);
            LoadStage(debugRequest, "Debug warp requested");
        }

        public RunStageLoadRequest GetCurrentStageLoadRequest(string sceneName) {
            EnsureRun();

            if (PendingStageLoadRequest != null && string.Equals(PendingStageLoadRequest.sceneName, sceneName, StringComparison.Ordinal)) {
                return CloneStageLoadRequest(PendingStageLoadRequest);
            }

            return CreateStageLoadRequest(CurrentRun.currentStageIndex, sceneName);
        }

        public string GetRunStateDebugString() {
            EnsureRun();

            string activeStageSummary = _activeStageResult == null
                ? "Stage log: inactive"
                : $"Stage log: {_activeStageResult.outcome} / {CurrentRun.currentStageElapsedTime:0.00}s / scene {_activeStageResult.sceneName}";

            string lastResultSummary = LastCompletedResult == null
                ? "Last result: none"
                : $"Last result: {LastCompletedResult.endReason} @ stage {LastCompletedResult.finalStageIndex}";

            return $"{CurrentRun.ToDebugString()}\n{activeStageSummary}\n{lastResultSummary}";
        }

        public void SetPendingDeathCause(string cause) {
            if (string.IsNullOrWhiteSpace(cause)) {
                return;
            }

            _pendingDeathCause = cause;
        }

        public void RecordPlayerDeath(Player player) {
            EnsureRun();
            if (CurrentRun.IsRunEnded || CurrentRun.progressState == RunProgressState.Transitioning) {
                Debug.LogWarning($"RunManager: Ignoring death record while run state is {CurrentRun.progressState}.");
                return;
            }

            CapturePlayerState(player);
            CurrentRun.progressState = RunProgressState.Death;

            string deathCause = string.IsNullOrWhiteSpace(_pendingDeathCause) ? "Unknown" : _pendingDeathCause;
            CompleteActiveStage("death", deathCause);
            FinalizeCurrentRun("death", deathCause);
        }

        public void RestartRun(string sceneName) {
            if (CurrentRun != null) {
                CurrentRun.progressState = RunProgressState.Restart;
            }

            if (CurrentResult != null && string.IsNullOrWhiteSpace(CurrentResult.endedAtUtc)) {
                CompleteActiveStage("restart");
                FinalizeCurrentRun("restart", string.Empty, false);
            }

            ResetRuntimeReferences();
            StartNewRun();

            RunStageLoadRequest restartRequest = GetCurrentStageLoadRequest(sceneName);
            PendingStageLoadRequest = CloneStageLoadRequest(restartRequest);
            ApplyStageLoadRequest(restartRequest);
            DismissResultUI();

            if (GameManager.Instance != null && GameManager.Instance.TryStartStageInPlace(restartRequest)) {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void StartNewRun() {
            ResetRuntimeReferences();
            CurrentRun = RunState.CreateDefault();
            CurrentResult = CreateRunResult(CurrentRun);
            Debug.Log($"RunManager: Created default run state for run {CurrentRun.runId}.");
        }

        private void EnterCurrentStage(string sceneName) {
            CurrentRun.progressState = RunProgressState.Active;
            StartStageTracking(sceneName);
        }

        private void BeginStageTransition() {
            CurrentRun.progressState = RunProgressState.Transitioning;
        }

        private RunStageLoadRequest CreateNextStageLoadRequest(string sceneName) {
            if (!CurrentRun.HasNextStage) {
                return null;
            }

            return CreateStageLoadRequest(CurrentRun.currentStageIndex + 1, sceneName);
        }

        private RunStageLoadRequest CreateStageLoadRequest(int stageIndex, string sceneName) {
            int clampedStageIndex = Mathf.Clamp(stageIndex, 1, CurrentRun.totalStageCount);
            return new RunStageLoadRequest {
                stageIndex = clampedStageIndex,
                stageId = RunState.CreateStageId(clampedStageIndex),
                sceneName = sceneName,
                isFinalStage = clampedStageIndex >= CurrentRun.totalStageCount
            };
        }

        private void LoadStage(RunStageLoadRequest loadRequest, string logPrefix = "Requested next stage") {
            if (loadRequest == null) {
                Debug.LogWarning("RunManager: Stage load request was null.");
                return;
            }

            PendingStageLoadRequest = CloneStageLoadRequest(loadRequest);
            ApplyStageLoadRequest(loadRequest);
            Debug.Log($"RunManager: {logPrefix}. Trying GameManager in-place transition for {loadRequest}.");

            if (GameManager.Instance != null && GameManager.Instance.TryStartStageInPlace(loadRequest)) {
                PendingStageLoadRequest = null;
                return;
            }

            Debug.Log($"RunManager: Falling back to temporary scene reload for {loadRequest}.");

            // Temporary implementation: keep using the current Game scene until
            // stage-specific scene/content selection is wired into the run flow.
            SceneManager.LoadScene(loadRequest.sceneName);
        }

        private void ApplyStageLoadRequest(RunStageLoadRequest loadRequest) {
            CurrentRun.currentStageIndex = loadRequest.stageIndex;
            CurrentRun.currentStageId = loadRequest.stageId;
            CurrentRun.currentStageElapsedTime = 0f;
            _pendingDeathCause = null;
        }

        private static RunStageLoadRequest CloneStageLoadRequest(RunStageLoadRequest loadRequest) {
            if (loadRequest == null) {
                return null;
            }

            return new RunStageLoadRequest {
                stageIndex = loadRequest.stageIndex,
                stageId = loadRequest.stageId,
                sceneName = loadRequest.sceneName,
                isFinalStage = loadRequest.isFinalStage
            };
        }

        private void CompleteRunClear(string sceneName) {
            CurrentRun.progressState = RunProgressState.Clear;
            FinalizeCurrentRun("clear", string.Empty);
            ShowRunClearResult(sceneName);
        }

        private void StartStageTracking(string sceneName) {
            if (CurrentResult == null) {
                CurrentResult = CreateRunResult(CurrentRun);
            }

            if (_activeStageResult != null) {
                Debug.LogWarning($"RunManager: Stage tracking already active for stage {_activeStageResult.stageIndex} in scene '{_activeStageResult.sceneName}'.");
                return;
            }

            _activeStageResult = new StageRunResult {
                stageIndex = CurrentRun.currentStageIndex,
                sceneName = sceneName,
                durationSeconds = 0f,
                outcome = "active",
                deathCause = string.Empty
            };

            CurrentRun.currentStageElapsedTime = 0f;
            CurrentResult.stageResults.Add(_activeStageResult);
        }

        private void CompleteActiveStage(string outcome, string deathCause = null) {
            if (_activeStageResult == null) {
                return;
            }

            _activeStageResult.durationSeconds = CurrentRun.currentStageElapsedTime;
            _activeStageResult.outcome = outcome;
            _activeStageResult.deathCause = deathCause ?? string.Empty;
            _activeStageResult = null;
        }

        private void FinalizeCurrentRun(string endReason, string deathCause, bool appendToHistory = true) {
            if (CurrentRun == null || CurrentResult == null) {
                return;
            }

            if (!string.IsNullOrWhiteSpace(CurrentResult.endedAtUtc)) {
                return;
            }

            CurrentResult.endedAtUtc = DateTime.UtcNow.ToString("O");
            CurrentResult.finalStageIndex = CurrentRun.currentStageIndex;
            CurrentResult.totalDurationSeconds = CurrentRun.elapsedTime;
            CurrentResult.finalHealth = CurrentRun.health;
            CurrentResult.finalBombs = CurrentRun.bombs;
            CurrentResult.finalRopes = CurrentRun.ropes;
            CurrentResult.finalGold = CurrentRun.gold;
            CurrentResult.endReason = endReason;
            CurrentResult.finalDeathCause = deathCause ?? string.Empty;

            LastCompletedResult = CurrentResult;

            if (appendToHistory) {
                _completedResults.Add(CurrentResult);
                AppendResultToDisk(CurrentResult);
            }

            Debug.Log($"RunManager: Finalized run.\n{CurrentResult.ToSummaryString()}");
        }

        private void ResetRuntimeReferences() {
            ActivePlayer = null;
            CurrentRun = null;
            CurrentResult = null;
            _activeStageResult = null;
            PendingStageLoadRequest = null;
            _pendingDeathCause = null;
        }

        private void ShowRunClearResult(string sceneName) {
            GameOverUI.ShowResult(new GameOverUI.ResultViewModel {
                Title = "RUN CLEAR",
                ValueLabel = "GOLD / TIME",
                ValueText = $"{CurrentRun.gold}\n{CurrentRun.elapsedTime:0.0}s",
                PrimaryActionLabel = "RESTART",
                PrimaryAction = () => RestartRun(sceneName)
            });
        }

        private void DismissResultUI() {
            if (GameOverUI.Instance == null) {
                return;
            }

            Destroy(GameOverUI.Instance.gameObject);
        }

        private RunResult CreateRunResult(RunState runState) {
            return new RunResult {
                runId = runState.runId,
                startedAtUtc = DateTime.UtcNow.ToString("O"),
                endedAtUtc = string.Empty,
                finalStageIndex = runState.currentStageIndex,
                totalDurationSeconds = 0f,
                finalHealth = runState.health,
                finalBombs = runState.bombs,
                finalRopes = runState.ropes,
                finalGold = runState.gold,
                endReason = "active",
                finalDeathCause = string.Empty,
                stageResults = new List<StageRunResult>()
            };
        }

        private void AppendResultToDisk(RunResult result) {
            try {
                string logPath = Path.Combine(Application.persistentDataPath, "run-results.jsonl");
                File.AppendAllText(logPath, JsonUtility.ToJson(result) + Environment.NewLine);
                LastRunResultLogPath = logPath;
            }
            catch (Exception exception) {
                Debug.LogWarning($"RunManager: Failed to persist run result log. {exception.Message}");
            }
        }

    }

}
