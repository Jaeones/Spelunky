using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spelunky {

    public class DebugManager : MonoBehaviour {

        public static DebugManager Instance { get; private set; }

        public bool RuntimeToolsEnabled { get; private set; }
        public bool IsOverlayVisible { get; private set; }
        public string LastRunStateDump { get; private set; }
        public DateTime SessionStartedUtc { get; private set; }
        public string SessionId { get; private set; }
        public string LastExportedSummaryPath { get; private set; }
        public string ActivePresetLabel { get; private set; }
        public string SessionRunType { get; private set; }
        public string SessionNote { get; private set; }
        public string SessionBuildLabel { get; private set; }
        public string TesterTag { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() {
            if (!Application.isEditor && !Debug.isDebugBuild) {
                return;
            }

            EnsureInstance();
        }

        public static DebugManager EnsureInstance() {
            if (Instance != null) {
                return Instance;
            }

            GameObject root = new GameObject("DebugManager");
            return root.AddComponent<DebugManager>();
        }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RuntimeToolsEnabled = Application.isEditor || Debug.isDebugBuild;
            IsOverlayVisible = false;
            InitializeSessionState();

            if (GetComponent<PlaytestDebugOverlay>() == null) {
                gameObject.AddComponent<PlaytestDebugOverlay>();
            }
        }

        public void ToggleOverlay() {
            if (!RuntimeToolsEnabled) {
                return;
            }

            IsOverlayVisible = !IsOverlayVisible;
        }

        public void DumpRunState() {
            if (RunManager.Instance == null) {
                LastRunStateDump = "RunManager unavailable.";
                return;
            }

            LastRunStateDump = RunManager.Instance.GetRunStateDebugString();
            Debug.Log($"DebugManager: RunState dump\n{LastRunStateDump}");
        }

        public void WarpToStage(int stageIndex) {
            if (!RuntimeToolsEnabled || RunManager.Instance == null) {
                return;
            }

            RunManager.Instance.DebugWarpToStage(stageIndex);
            DumpRunState();
        }

        public void ForceSetResources(int health, int bombs, int ropes) {
            if (!RuntimeToolsEnabled || RunManager.Instance == null) {
                return;
            }

            RunManager.Instance.ForceSetResources(health, bombs, ropes);
            DumpRunState();
        }

        public void AddAccessory(AccessoryType accessoryType) {
            if (!RuntimeToolsEnabled) {
                return;
            }

            Player player = ResolveActivePlayer();
            if (player == null || player.Accessories == null) {
                LastRunStateDump = "Active player unavailable.";
                return;
            }

            if (player.Accessories.HasAccessory(accessoryType)) {
                LastRunStateDump = $"{accessoryType} already owned.";
                Debug.Log($"DebugManager: Skipped duplicate accessory {accessoryType}.");
                return;
            }

            Sprite icon = ResolveAccessoryIcon(accessoryType);
            player.Accessories.AddAccessory(accessoryType, icon);

            if (RunManager.Instance != null) {
                RunManager.Instance.CapturePlayerState(player);
            }

            DumpRunState();
        }

        public void OpenEndingScene() {
            if (!RuntimeToolsEnabled) {
                return;
            }

            SceneManager.LoadScene("Ending");
        }

        public string GetLastRunResultSummary() {
            if (RunManager.Instance == null || RunManager.Instance.LastCompletedResult == null) {
                return "No completed run recorded in this session.";
            }

            return RunManager.Instance.LastCompletedResult.ToSummaryString();
        }

        public string GetRunResultLogPath() {
            if (RunManager.Instance != null && !string.IsNullOrWhiteSpace(RunManager.Instance.LastRunResultLogPath)) {
                return RunManager.Instance.LastRunResultLogPath;
            }

            return Path.Combine(Application.persistentDataPath, "run-results.jsonl");
        }

        public string GetRunLogOverview() {
            return RunLogAnalyzer.BuildOverview(GetRunResultLogPath());
        }

        public string GetSessionRunLogOverview() {
            return RunLogAnalyzer.BuildSessionOverview(GetRunResultLogPath(), SessionStartedUtc);
        }

        public string GetRecentRunLogSummary() {
            return RunLogAnalyzer.BuildRecentRuns(GetRunResultLogPath());
        }

        public string GetRecentRunLogStats() {
            return RunLogAnalyzer.BuildRecentStats(GetRunResultLogPath());
        }

        public string GetRecentHighlights() {
            return RunLogAnalyzer.BuildRecentHighlights(GetRunResultLogPath());
        }

        public void ExportQaSummary() {
            LastExportedSummaryPath = RunLogSummaryExporter.ExportMarkdownSummary(
                GetRunResultLogPath(),
                SessionStartedUtc,
                SessionId,
                ActivePresetLabel,
                SessionRunType,
                SessionNote,
                SessionBuildLabel,
                TesterTag
            );
            Debug.Log($"DebugManager: Exported QA summary to {LastExportedSummaryPath}");
        }

        public void BeginNewSession() {
            if (RunLogAnalyzer.CountSessionRuns(GetRunResultLogPath(), SessionStartedUtc) > 0) {
                LastExportedSummaryPath = RunLogSummaryExporter.ExportMarkdownSummary(
                    GetRunResultLogPath(),
                    SessionStartedUtc,
                    SessionId,
                    ActivePresetLabel,
                    SessionRunType,
                    SessionNote,
                    SessionBuildLabel,
                    TesterTag
                );
                Debug.Log($"DebugManager: Auto-exported previous session to {LastExportedSummaryPath}");
            }

            InitializeSessionState();
            Debug.Log($"DebugManager: Started new QA session {SessionId} at {SessionStartedUtc:O}");
        }

        public void SetActivePresetLabel(string presetLabel) {
            ActivePresetLabel = string.IsNullOrWhiteSpace(presetLabel) ? "none" : presetLabel;
        }

        public void SetSessionRunType(string runType) {
            SessionRunType = string.IsNullOrWhiteSpace(runType) ? "unspecified" : runType;
        }

        public void SetSessionNote(string note) {
            SessionNote = note ?? string.Empty;
        }

        public void SetSessionBuildLabel(string buildLabel) {
            SessionBuildLabel = string.IsNullOrWhiteSpace(buildLabel) ? GetDefaultBuildLabel() : buildLabel;
        }

        public void SetTesterTag(string testerTag) {
            TesterTag = string.IsNullOrWhiteSpace(testerTag) ? GetDefaultTesterTag() : testerTag;
        }

        private void InitializeSessionState() {
            LastRunStateDump = "Run state unavailable.";
            SessionStartedUtc = DateTime.UtcNow;
            SessionId = SessionStartedUtc.ToString("yyyyMMdd_HHmmss");
            LastExportedSummaryPath = "No summary exported.";
            ActivePresetLabel = "none";
            SessionRunType = "unspecified";
            SessionNote = string.Empty;
            SessionBuildLabel = GetDefaultBuildLabel();
            TesterTag = GetDefaultTesterTag();
        }

        private static string GetDefaultBuildLabel() {
            return string.IsNullOrWhiteSpace(Application.version) ? "dev-local" : Application.version;
        }

        private static string GetDefaultTesterTag() {
            return string.IsNullOrWhiteSpace(Environment.UserName) ? "unknown" : Environment.UserName;
        }

        private Player ResolveActivePlayer() {
            if (RunManager.Instance != null && RunManager.Instance.ActivePlayer != null) {
                return RunManager.Instance.ActivePlayer;
            }

            if (GameManager.Instance != null && GameManager.Instance.ActivePlayer != null) {
                return GameManager.Instance.ActivePlayer;
            }

            return FindObjectOfType<Player>();
        }

        private Sprite ResolveAccessoryIcon(AccessoryType accessoryType) {
            AccessoryPickup[] pickups = FindObjectsOfType<AccessoryPickup>();
            for (int i = 0; i < pickups.Length; i++) {
                AccessoryPickup pickup = pickups[i];
                if (pickup == null || pickup.accessoryType != accessoryType) {
                    continue;
                }

                if (pickup.icon != null) {
                    return pickup.icon;
                }

                SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null) {
                    return renderer.sprite;
                }
            }

#if UNITY_EDITOR
            GameObject accessoryPrefab = LoadAccessoryPrefab(accessoryType);
            if (accessoryPrefab != null) {
                AccessoryPickup pickup = accessoryPrefab.GetComponent<AccessoryPickup>();
                if (pickup != null && pickup.icon != null) {
                    return pickup.icon;
                }

                SpriteRenderer renderer = accessoryPrefab.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null) {
                    return renderer.sprite;
                }
            }
#endif

            return null;
        }

#if UNITY_EDITOR
        private static GameObject LoadAccessoryPrefab(AccessoryType accessoryType) {
            string prefabPath;
            switch (accessoryType) {
                case AccessoryType.ClimbingGlove:
                    prefabPath = "Assets/Prefabs/Items/Accessories/ClimbingGlove.prefab";
                    break;
                case AccessoryType.SpringBoots:
                    prefabPath = "Assets/Prefabs/Items/Accessories/SpringBoots.prefab";
                    break;
                case AccessoryType.PitchersMitt:
                    prefabPath = "Assets/Prefabs/Items/Accessories/PitchersMitt.prefab";
                    break;
                case AccessoryType.Paste:
                    prefabPath = "Assets/Prefabs/Items/Accessories/Paste.prefab";
                    break;
                default:
                    return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
#endif
    }

    public static class DebugDamageContext {

        public const string UnknownCause = "Unknown";

        [ThreadStatic] private static string _currentCause;

        public static string CurrentCause => string.IsNullOrWhiteSpace(_currentCause) ? UnknownCause : _currentCause;

        public static Scope Use(string cause) {
            return new Scope(cause);
        }

        public readonly struct Scope : IDisposable {

            private readonly string _previousCause;

            public Scope(string cause) {
                _previousCause = _currentCause;
                _currentCause = string.IsNullOrWhiteSpace(cause) ? UnknownCause : cause;
            }

            public void Dispose() {
                _currentCause = _previousCause;
            }
        }
    }

}
