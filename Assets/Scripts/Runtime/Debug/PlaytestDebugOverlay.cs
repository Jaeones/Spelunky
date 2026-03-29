using UnityEngine;

namespace Spelunky {

    public class PlaytestDebugOverlay : MonoBehaviour {

        private readonly struct PlaytestPreset {

            public readonly string Label;
            public readonly int StageIndex;
            public readonly int Health;
            public readonly int Bombs;
            public readonly int Ropes;
            public readonly string Goal;

            public PlaytestPreset(string label, int stageIndex, int health, int bombs, int ropes, string goal) {
                Label = label;
                StageIndex = stageIndex;
                Health = health;
                Bombs = bombs;
                Ropes = ropes;
                Goal = goal;
            }
        }

        private static readonly PlaytestPreset[] DefaultPresets = {
            new PlaytestPreset("Stage 1 Baseline", 1, 4, 4, 4, "Entry Mine baseline check"),
            new PlaytestPreset("Stage 2 Rope Pressure", 2, 4, 4, 2, "Check rope pressure without changing core tuning"),
            new PlaytestPreset("Stage 3 Bomb Pressure", 3, 3, 2, 3, "Check bomb value and shortcut pressure"),
            new PlaytestPreset("Stage 4 Clutch", 4, 2, 1, 1, "Check final-stage resource squeeze")
        };

        private Rect _windowRect = new Rect(12f, 12f, 420f, 520f);
        private Vector2 _scrollPosition;
        private string _stageInput = "1";
        private string _healthInput = "4";
        private string _bombInput = "4";
        private string _ropeInput = "4";
        private string _sessionNote = string.Empty;
        private string _sessionBuildLabel = string.Empty;
        private string _testerTag = string.Empty;
        private PlaytestPreset[] _presets;
        private string[] _runTypes;

        private void Awake() {
            LoadPresets();
            LoadRunTypes();
            SyncSessionFieldsFromManager();
        }

        private void Update() {
            if (DebugManager.Instance == null || !DebugManager.Instance.RuntimeToolsEnabled) {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1)) {
                DebugManager.Instance.ToggleOverlay();
            }

            if (Input.GetKeyDown(KeyCode.F2)) {
                DebugManager.Instance.DumpRunState();
            }

            if (Input.GetKeyDown(KeyCode.F5)) {
                ApplyPreset(_presets[0]);
            }

            if (Input.GetKeyDown(KeyCode.F6)) {
                DebugManager.Instance.WarpToStage(1);
            }

            if (Input.GetKeyDown(KeyCode.F7)) {
                DebugManager.Instance.WarpToStage(2);
            }

            if (Input.GetKeyDown(KeyCode.F8)) {
                DebugManager.Instance.WarpToStage(3);
            }

            if (Input.GetKeyDown(KeyCode.F9)) {
                DebugManager.Instance.WarpToStage(4);
            }

            if (Input.GetKeyDown(KeyCode.F10)) {
                DebugManager.Instance.OpenEndingScene();
            }
        }

        private void OnGUI() {
            if (DebugManager.Instance == null || !DebugManager.Instance.RuntimeToolsEnabled || !DebugManager.Instance.IsOverlayVisible) {
                return;
            }

            _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Playtest Debug");
        }

        private void DrawWindow(int windowId) {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
            GUILayout.BeginVertical();

            DrawSummary();
            DrawWarpControls();
            DrawResourceControls();
            DrawPresetControls();
            DrawLoggingControls();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 24f));
        }

        private void DrawSummary() {
            GUILayout.Label("Hotkeys: F1 Toggle, F2 Dump, F5 Baseline, F6-F9 Warp, F10 Ending");

            RunManager runManager = RunManager.Instance;
            if (runManager == null || runManager.CurrentRun == null) {
                GUILayout.Label("RunManager unavailable");
                return;
            }

            RunState run = runManager.CurrentRun;
            GUILayout.Label($"Current Stage: {run.currentStageIndex} ({GetTargetTimeLabel(run.currentStageIndex)})");
            GUILayout.Label($"Run Time: {run.elapsedTime:0.0}s | Stage Time: {run.currentStageElapsedTime:0.0}s");
            GUILayout.Label($"Health {run.health}/{run.maxHealth} | Bombs {run.bombs} | Ropes {run.ropes} | Gold {run.gold}");
            GUILayout.Label($"Active Preset: {DebugManager.Instance.ActivePresetLabel}");
            GUILayout.Label($"Run Type: {DebugManager.Instance.SessionRunType}");
            GUILayout.Label($"Build: {DebugManager.Instance.SessionBuildLabel}");
            GUILayout.Label($"Tester: {DebugManager.Instance.TesterTag}");
            GUILayout.Label($"Session ID: {DebugManager.Instance.SessionId}");
            GUILayout.Label($"Session Started: {DebugManager.Instance.SessionStartedUtc:HH:mm:ss}");
        }

        private void DrawWarpControls() {
            GUILayout.Space(8f);
            GUILayout.Label("Stage Warp");
            _stageInput = GUILayout.TextField(_stageInput, 4);

            if (GUILayout.Button("Warp To Stage")) {
                if (int.TryParse(_stageInput, out int stageIndex)) {
                    DebugManager.Instance.WarpToStage(stageIndex);
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stage 1")) {
                DebugManager.Instance.WarpToStage(1);
            }

            if (GUILayout.Button("Stage 2")) {
                DebugManager.Instance.WarpToStage(2);
            }

            if (GUILayout.Button("Stage 3")) {
                DebugManager.Instance.WarpToStage(3);
            }

            if (GUILayout.Button("Stage 4")) {
                DebugManager.Instance.WarpToStage(4);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Open Ending Scene")) {
                DebugManager.Instance.OpenEndingScene();
            }
        }

        private void DrawResourceControls() {
            GUILayout.Space(8f);
            GUILayout.Label("Force Resources");
            GUILayout.BeginHorizontal();
            GUILayout.Label("HP", GUILayout.Width(24f));
            _healthInput = GUILayout.TextField(_healthInput, 4);
            GUILayout.Label("Bomb", GUILayout.Width(40f));
            _bombInput = GUILayout.TextField(_bombInput, 4);
            GUILayout.Label("Rope", GUILayout.Width(40f));
            _ropeInput = GUILayout.TextField(_ropeInput, 4);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Apply HP / Bomb / Rope")) {
                if (TryParseResourceInputs(out int health, out int bombs, out int ropes)) {
                    DebugManager.Instance.ForceSetResources(health, bombs, ropes);
                }
            }
        }

        private void DrawPresetControls() {
            GUILayout.Space(8f);
            GUILayout.Label("Quick Presets");

            for (int i = 0; i < _presets.Length; i++) {
                PlaytestPreset preset = _presets[i];
                if (GUILayout.Button($"{preset.Label} ({preset.Health}/{preset.Bombs}/{preset.Ropes})")) {
                    ApplyPreset(preset);
                }

                GUILayout.Label(preset.Goal);
            }
        }

        private void DrawLoggingControls() {
            GUILayout.Space(8f);
            GUILayout.Label("RunState Dump");

            if (GUILayout.Button("Begin New QA Session")) {
                DebugManager.Instance.BeginNewSession();
                SyncSessionFieldsFromManager();
            }

            if (GUILayout.Button("Dump RunState To Console")) {
                DebugManager.Instance.DumpRunState();
            }

            GUILayout.TextArea(DebugManager.Instance.LastRunStateDump, GUILayout.MinHeight(110f));

            GUILayout.Space(8f);
            GUILayout.Label("Result Log");
            GUILayout.TextArea(DebugManager.Instance.GetLastRunResultSummary(), GUILayout.MinHeight(110f));
            GUILayout.Label(DebugManager.Instance.GetRunResultLogPath());

            GUILayout.Space(8f);
            GUILayout.Label("Historical Log Overview");
            GUILayout.TextArea(DebugManager.Instance.GetRunLogOverview(), GUILayout.MinHeight(110f));

            GUILayout.Space(8f);
            GUILayout.Label("Session Overview");
            GUILayout.TextArea(DebugManager.Instance.GetSessionRunLogOverview(), GUILayout.MinHeight(110f));

            GUILayout.Space(8f);
            GUILayout.Label("Recent Run Stats");
            GUILayout.TextArea(DebugManager.Instance.GetRecentRunLogStats(), GUILayout.MinHeight(100f));

            GUILayout.Space(8f);
            GUILayout.Label("Recent Highlights");
            GUILayout.TextArea(DebugManager.Instance.GetRecentHighlights(), GUILayout.MinHeight(80f));

            GUILayout.Space(8f);
            GUILayout.Label("Recent Runs");
            GUILayout.TextArea(DebugManager.Instance.GetRecentRunLogSummary(), GUILayout.MinHeight(140f));

            GUILayout.Space(8f);
            GUILayout.Label("Run Type");
            for (int i = 0; i < _runTypes.Length; i++) {
                string runType = _runTypes[i];
                if (GUILayout.Button(runType)) {
                    DebugManager.Instance.SetSessionRunType(runType);
                }
            }

            GUILayout.Space(8f);
            GUILayout.Label("Session Note");
            string nextNote = GUILayout.TextField(_sessionNote);
            if (nextNote != _sessionNote) {
                _sessionNote = nextNote;
                DebugManager.Instance.SetSessionNote(_sessionNote);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Session Build");
            string nextBuildLabel = GUILayout.TextField(_sessionBuildLabel);
            if (nextBuildLabel != _sessionBuildLabel) {
                _sessionBuildLabel = nextBuildLabel;
                DebugManager.Instance.SetSessionBuildLabel(_sessionBuildLabel);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Tester Tag");
            string nextTesterTag = GUILayout.TextField(_testerTag);
            if (nextTesterTag != _testerTag) {
                _testerTag = nextTesterTag;
                DebugManager.Instance.SetTesterTag(_testerTag);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Export QA Summary Markdown")) {
                DebugManager.Instance.ExportQaSummary();
            }

            GUILayout.Label(DebugManager.Instance.LastExportedSummaryPath);
        }

        private void ApplyPreset(PlaytestPreset preset) {
            _stageInput = preset.StageIndex.ToString();
            _healthInput = preset.Health.ToString();
            _bombInput = preset.Bombs.ToString();
            _ropeInput = preset.Ropes.ToString();
            DebugManager.Instance.SetActivePresetLabel(preset.Label);

            DebugManager.Instance.WarpToStage(preset.StageIndex);
            DebugManager.Instance.ForceSetResources(preset.Health, preset.Bombs, preset.Ropes);
        }

        private bool TryParseResourceInputs(out int health, out int bombs, out int ropes) {
            bool hasHealth = int.TryParse(_healthInput, out health);
            bool hasBombs = int.TryParse(_bombInput, out bombs);
            bool hasRopes = int.TryParse(_ropeInput, out ropes);
            return hasHealth && hasBombs && hasRopes;
        }

        private void LoadPresets() {
            PlaytestDebugPresetCatalog catalog = Resources.Load<PlaytestDebugPresetCatalog>(PlaytestDebugPresetCatalog.ResourcesPath);
            if (catalog == null || catalog.presets == null || catalog.presets.Length == 0) {
                _presets = DefaultPresets;
                return;
            }

            _presets = new PlaytestPreset[catalog.presets.Length];
            for (int i = 0; i < catalog.presets.Length; i++) {
                PlaytestDebugPresetData preset = catalog.presets[i];
                _presets[i] = new PlaytestPreset(
                    string.IsNullOrWhiteSpace(preset.label) ? $"Preset {i + 1}" : preset.label,
                    Mathf.Max(1, preset.stageIndex),
                    Mathf.Max(1, preset.health),
                    Mathf.Max(0, preset.bombs),
                    Mathf.Max(0, preset.ropes),
                    string.IsNullOrWhiteSpace(preset.goal) ? "No goal description." : preset.goal
                );
            }

            if (_presets.Length == 0) {
                _presets = DefaultPresets;
            }
        }

        private void LoadRunTypes() {
            PlaytestRunTypeCatalog catalog = Resources.Load<PlaytestRunTypeCatalog>(PlaytestRunTypeCatalog.ResourcesPath);
            if (catalog == null || catalog.runTypes == null || catalog.runTypes.Length == 0) {
                _runTypes = new[] {
                    "first-clear",
                    "cautious full-clear",
                    "greedy treasure",
                    "speed-focused",
                    "controller-only",
                    "keyboard-only"
                };
                return;
            }

            _runTypes = catalog.runTypes;
        }

        private void SyncSessionFieldsFromManager() {
            if (DebugManager.Instance == null) {
                return;
            }

            _sessionNote = DebugManager.Instance.SessionNote;
            _sessionBuildLabel = DebugManager.Instance.SessionBuildLabel;
            _testerTag = DebugManager.Instance.TesterTag;
        }

        private string GetTargetTimeLabel(int stageIndex) {
            switch (Mathf.Clamp(stageIndex, 1, 4)) {
                case 1:
                    return "Target 3-5m";
                case 2:
                    return "Target 4-6m";
                case 3:
                    return "Target 4-7m";
                case 4:
                    return "Target 5-8m";
                default:
                    return "Target n/a";
            }
        }
    }

}
