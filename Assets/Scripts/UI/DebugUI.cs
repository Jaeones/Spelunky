using UnityEngine;

namespace Spelunky {

    public class DebugUI : MonoBehaviour {

        private const float PanelWidth = 360f;
        private const float PanelHeight = 310f;

        private Rect _windowRect = new Rect(12f, 12f, PanelWidth, PanelHeight);
        private Vector2 _scrollPosition;
        private string _stageInput = "1";
        private string _healthInput = "4";
        private string _bombInput = "4";
        private string _ropeInput = "4";

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
                DebugManager.Instance.ForceSetResources(4, 4, 4);
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

            _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Debug Tools");
        }

        private void DrawWindow(int windowId) {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
            GUILayout.BeginVertical();

            GUILayout.Label("Hotkeys: F1 Toggle, F2 Dump, F5 Baseline, F6-F9 Warp, F10 Ending");

            RunManager runManager = RunManager.Instance;
            if (runManager != null && runManager.CurrentRun != null) {
                GUILayout.Label($"Stage {runManager.CurrentRun.currentStageIndex} | Run {runManager.CurrentRun.elapsedTime:0.0}s | Stage {runManager.CurrentRun.currentStageElapsedTime:0.0}s");
                GUILayout.Label($"HP {runManager.CurrentRun.health}/{runManager.CurrentRun.maxHealth} | Bombs {runManager.CurrentRun.bombs} | Ropes {runManager.CurrentRun.ropes} | Gold {runManager.CurrentRun.gold}");
            }
            else {
                GUILayout.Label("RunManager unavailable");
            }

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

            GUILayout.Space(8f);
            GUILayout.Label("Force Resources");
            _healthInput = GUILayout.TextField(_healthInput, 4);
            _bombInput = GUILayout.TextField(_bombInput, 4);
            _ropeInput = GUILayout.TextField(_ropeInput, 4);

            if (GUILayout.Button("Apply HP / Bomb / Rope")) {
                if (TryParseInputs(out int health, out int bombs, out int ropes)) {
                    DebugManager.Instance.ForceSetResources(health, bombs, ropes);
                }
            }

            if (GUILayout.Button("Apply Baseline 4 / 4 / 4")) {
                _healthInput = "4";
                _bombInput = "4";
                _ropeInput = "4";
                DebugManager.Instance.ForceSetResources(4, 4, 4);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Dump RunState To Console")) {
                DebugManager.Instance.DumpRunState();
            }

            GUILayout.TextArea(DebugManager.Instance.LastRunStateDump, GUILayout.MinHeight(90f));

            if (runManager != null && runManager.LastCompletedResult != null) {
                GUILayout.Space(8f);
                GUILayout.Label("Last Run Result");
                GUILayout.TextArea(runManager.LastCompletedResult.ToSummaryString(), GUILayout.MinHeight(90f));
                if (!string.IsNullOrWhiteSpace(runManager.LastRunResultLogPath)) {
                    GUILayout.Label(runManager.LastRunResultLogPath);
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private bool TryParseInputs(out int health, out int bombs, out int ropes) {
            bool isHealthValid = int.TryParse(_healthInput, out health);
            bool isBombValid = int.TryParse(_bombInput, out bombs);
            bool isRopeValid = int.TryParse(_ropeInput, out ropes);
            return isHealthValid && isBombValid && isRopeValid;
        }
    }

}
