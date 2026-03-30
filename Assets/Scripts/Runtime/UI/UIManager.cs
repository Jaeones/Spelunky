using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spelunky {

    /// <summary>
    /// Central entry point for game-wide UI flow.
    /// This is intentionally minimal until HUD, transition, and result screens are migrated.
    /// </summary>
    public class UIManager : MonoBehaviour {

        private const int TemporaryTransitionSortingOrder = 90;
        private static readonly Color TemporaryTransitionOverlayColor = new Color(0f, 0f, 0f, 0.65f);

        public enum UIFlowState {
            Gameplay,
            Transition,
            Result
        }

        [System.Serializable]
        public struct TransitionViewModel {
            public string Title;
            public string Detail;
        }

        public static TransitionViewModel CreateStageTransitionModel(int stageIndex, string stageName) {
            UIManager manager = EnsureInstance();
            if (manager != null) {
                return manager.CreateConfiguredStageTransitionModel(stageIndex, stageName);
            }

            string title = stageIndex > 0 ? $"STAGE {stageIndex}" : "STAGE";
            string detail = string.IsNullOrEmpty(stageName) ? string.Empty : stageName;

            return new TransitionViewModel {
                Title = title,
                Detail = detail
            };
        }

        public static UIManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private PlayerHUDReferences playerHUD;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject transitionRoot;
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private GameOverUI resultScreen;

        [Header("Transition Binding")]
        [SerializeField] private Text transitionTitleText;
        [SerializeField] private Text transitionDetailText;

        [Header("Transition Text")]
        [SerializeField] private string defaultStageTransitionTitle = "STAGE";
        [SerializeField] private string stageTransitionTitleFormat = "STAGE {0}";

        public PlayerHUDReferences PlayerHUD => playerHUD;
        public GameOverUI ResultScreen => resultScreen;
        public UIFlowState CurrentState { get; private set; } = UIFlowState.Gameplay;
        public bool IsSettingsOpen => settingsUI != null && settingsUI.IsVisible;

        private SettingUI settingsUI;
        private float settingsPauseRestoreTimeScale = 1f;
        private bool ownsSettingsPause;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            if (FindObjectOfType<GameManager>() == null) {
                return;
            }

            EnsureInstance();
        }

        public static UIManager EnsureInstance() {
            if (Instance != null) {
                return Instance;
            }

            UIManager existing = FindObjectOfType<UIManager>();
            if (existing != null) {
                Instance = existing;
                return existing;
            }

            GameObject root = new GameObject("UIManager");
            return root.AddComponent<UIManager>();
        }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveSceneReferences();
        }

        private void OnDestroy() {
            ResumeGameplayAfterSettings();

            if (Instance == this) {
                Instance = null;
            }
        }

        private void Update() {
            if (!Input.GetKeyDown(KeyCode.Escape)) {
                return;
            }

            if (IsSettingsOpen) {
                SetSettingsPanelVisible(false);
                return;
            }

            if (CurrentState != UIFlowState.Gameplay) {
                return;
            }

            ToggleSettings();
        }

        public void ShowHUD(bool visible) {
            ResolveSceneReferences();

            if (hudRoot != null) {
                hudRoot.SetActive(visible);
            }
        }

        public void ShowTransition(bool visible) {
            ResolveSceneReferences();

            if (visible) {
                EnsureTransitionPresentation();
            }

            if (transitionRoot != null) {
                transitionRoot.SetActive(visible);
            }
        }

        public void ShowTransition(TransitionViewModel transition) {
            EnterTransitionState(transition);
        }

        public void ShowTransition(string title, string detail) {
            ShowTransition(new TransitionViewModel {
                Title = title,
                Detail = detail
            });
        }

        public void ShowStageTransition(int stageIndex, string stageName) {
            ShowTransition(CreateStageTransitionModel(stageIndex, stageName));
        }

        public void HideTransition() {
            if (CurrentState == UIFlowState.Transition) {
                EnterGameplayState();
                return;
            }

            ShowTransition(false);
            ClearTransition();
        }

        public void ShowResult(bool visible) {
            ResolveSceneReferences();

            if (resultRoot != null) {
                resultRoot.SetActive(visible);
            }
        }

        public bool TryShowGameOver(int score) {
            return TryShowGameOver(score, null);
        }

        public bool TryShowGameOver(int score, string restartSceneName) {
            ResolveSceneReferences();
            if (resultScreen == null) {
                return false;
            }

            ShowGameOverScreen(score, restartSceneName);
            return true;
        }

        public bool TryShowResult(GameOverUI.ResultViewModel result) {
            ResolveSceneReferences();
            if (resultScreen == null) {
                return false;
            }

            ShowResultScreen(result);
            return true;
        }

        public bool TryShowRunClear(int gold, float elapsedSeconds, string restartSceneName) {
            ResolveSceneReferences();
            if (resultScreen == null) {
                return false;
            }

            ShowRunClearScreen(gold, elapsedSeconds, restartSceneName);
            return true;
        }

        public void HideResultScreen() {
            ResolveSceneReferences();

            if (resultScreen != null) {
                resultScreen.HideResult();
            }

            EnterGameplayState();
        }

        public void ShowGameOverScreen(int score) {
            ShowGameOverScreen(score, null);
        }

        public void ShowGameOverScreen(int score, string restartSceneName) {
            ResolveSceneReferences();
            if (resultScreen == null) {
                return;
            }

            EnterResultState();
            string resolvedSceneName = string.IsNullOrEmpty(restartSceneName) ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : restartSceneName;
            resultScreen.ShowGameOverScreen(score, resolvedSceneName);
        }

        public void ShowResultScreen(GameOverUI.ResultViewModel result) {
            ResolveSceneReferences();
            if (resultScreen == null) {
                return;
            }

            EnterResultState();
            resultScreen.Show(result);
        }

        public void ShowRunClearScreen(int gold, float elapsedSeconds, string restartSceneName) {
            ResolveSceneReferences();
            if (resultScreen == null) {
                return;
            }

            EnterResultState();
            resultScreen.ShowRunClearScreen(gold, elapsedSeconds, restartSceneName);
        }

        public void ResetGameplayUI() {
            ResolveSceneReferences();

            if (resultScreen != null) {
                resultScreen.HideResult();
            }

            SetSettingsPanelVisible(false);
            EnterGameplayState();
        }

        public void ToggleSettings() {
            EnsureSettingsPresentation();
            SetSettingsPanelVisible(!IsSettingsOpen);
        }

        private void ResolveSceneReferences() {
            if (playerHUD == null) {
                playerHUD = FindObjectOfType<PlayerHUDReferences>();
            }

            if (hudRoot == null && playerHUD != null) {
                hudRoot = playerHUD.CanvasRoot;
            }

            if (resultScreen == null) {
                resultScreen = FindObjectOfType<GameOverUI>();
            }

            if (resultRoot == null && resultScreen != null) {
                resultRoot = resultScreen.gameObject;
            }
        }

        private void EnterGameplayState() {
            CurrentState = UIFlowState.Gameplay;

            ShowResult(false);
            ShowTransition(false);
            ClearTransition();
            ShowHUD(true);
        }

        private void EnterTransitionState(TransitionViewModel transition) {
            SetSettingsPanelVisible(false);
            CurrentState = UIFlowState.Transition;

            ShowResult(false);
            ShowHUD(false);
            ApplyTransition(transition);
            ShowTransition(true);
        }

        private void EnterResultState() {
            SetSettingsPanelVisible(false);
            CurrentState = UIFlowState.Result;

            ShowHUD(false);
            ShowTransition(false);
            ClearTransition();
            ShowResult(true);
        }

        private void ApplyTransition(TransitionViewModel transition) {
            if (transitionTitleText != null) {
                transitionTitleText.text = string.IsNullOrEmpty(transition.Title) ? string.Empty : transition.Title;
            }

            if (transitionDetailText != null) {
                transitionDetailText.text = string.IsNullOrEmpty(transition.Detail) ? string.Empty : transition.Detail;
            }
        }

        private void ClearTransition() {
            if (transitionTitleText != null) {
                transitionTitleText.text = string.Empty;
            }

            if (transitionDetailText != null) {
                transitionDetailText.text = string.Empty;
            }
        }

        private void EnsureSettingsPresentation() {
            if (settingsUI != null) {
                return;
            }

            EnsureEventSystem();
            SettingUI[] existingSettingUIs = FindObjectsOfType<SettingUI>(true);
            settingsUI = existingSettingUIs.Length > 0 ? existingSettingUIs[0] : null;
            if (settingsUI == null) {
                settingsUI = InstantiateSettingsUI();
            }

            if (settingsUI != null) {
                settingsUI.SetVisible(false);
            }
        }

        private void SetSettingsPanelVisible(bool isVisible) {
            if (settingsUI == null) {
                return;
            }

            bool wasVisible = settingsUI.IsVisible;
            if (isVisible && !wasVisible && CurrentState == UIFlowState.Gameplay) {
                PauseGameplayForSettings();
            }
            else if (!isVisible && wasVisible) {
                ResumeGameplayAfterSettings();
            }

            settingsUI.SetVisible(isVisible);

            if (!isVisible && EventSystem.current != null) {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void PauseGameplayForSettings() {
            if (ownsSettingsPause) {
                return;
            }

            settingsPauseRestoreTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            ownsSettingsPause = true;
        }

        private void ResumeGameplayAfterSettings() {
            if (!ownsSettingsPause) {
                return;
            }

            Time.timeScale = settingsPauseRestoreTimeScale;
            ownsSettingsPause = false;
        }

        private void EnsureTransitionPresentation() {
            if (transitionRoot != null) {
                return;
            }

            CreateTemporaryTransitionUI();
        }

        private TransitionViewModel CreateConfiguredStageTransitionModel(int stageIndex, string stageName) {
            string title = stageIndex > 0
                ? string.Format(stageTransitionTitleFormat, stageIndex)
                : defaultStageTransitionTitle;

            return new TransitionViewModel {
                Title = title,
                Detail = string.IsNullOrEmpty(stageName) ? string.Empty : stageName
            };
        }

        private void CreateTemporaryTransitionUI() {
            // Temporary runtime-generated transition overlay until a scene or prefab implementation is wired.
            GameObject root = new GameObject("TemporaryTransitionUI");
            root.transform.SetParent(transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = TemporaryTransitionSortingOrder;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 2f;
            scaler.referencePixelsPerUnit = 1f;

            root.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = TemporaryTransitionOverlayColor;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            transitionTitleText = CreateTemporaryTransitionText("Title", panel.transform, font, 18, FontStyle.Bold, new Vector2(0f, 16f));
            transitionDetailText = CreateTemporaryTransitionText("Detail", panel.transform, font, 10, FontStyle.Normal, new Vector2(0f, -10f));

            transitionRoot = root;
            transitionRoot.SetActive(false);
        }

        private SettingUI InstantiateSettingsUI() {
            GameObject settingsPrefab = AudioManager.Instance != null ? AudioManager.Instance.SettingsUIPrefab : null;
            if (settingsPrefab == null) {
                return null;
            }

            Transform parent = FindSettingsParent();
            GameObject instance = parent != null
                ? Instantiate(settingsPrefab, parent, false)
                : Instantiate(settingsPrefab);

            instance.name = settingsPrefab.name;

            SettingUI settingComponent = instance.GetComponent<SettingUI>();
            if (settingComponent == null) {
                settingComponent = instance.AddComponent<SettingUI>();
            }

            return settingComponent;
        }

        private Transform FindSettingsParent() {
            ResolveSceneReferences();

            if (playerHUD != null) {
                return playerHUD.transform;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }

        private static void EnsureEventSystem() {
            if (FindObjectOfType<EventSystem>() != null) {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static Text CreateTemporaryTransitionText(string name, Transform parent, Font font, int fontSize, FontStyle fontStyle, Vector2 anchoredPosition) {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(240f, 28f);

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = string.Empty;

            return text;
        }

    }

}
