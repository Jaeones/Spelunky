using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Spelunky {

    public class GameOverUI : MonoBehaviour {

        public enum ResultPreset {
            Custom,
            GameOver,
            RunClear
        }

        public sealed class ResultViewModel {
            public ResultPreset Preset;
            public string Title;
            public string ValueLabel;
            public string ValueText;
            public string PrimaryActionLabel;
            public UnityAction PrimaryAction;
        }

        private const int DefaultSortingOrder = 100;
        private const float DefaultScaleFactor = 2f;
        private static readonly Color DefaultOverlayColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color DefaultButtonColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        private static readonly Color DefaultButtonHighlightColor = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        private static readonly Color DefaultButtonPressedColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        public static GameOverUI Instance { get; private set; }

        [Header("Style")]
        [SerializeField] private Font defaultFont;
        [SerializeField] private Color overlayColor = DefaultOverlayColor;
        [SerializeField] private Color buttonColor = DefaultButtonColor;
        [SerializeField] private Color buttonHighlightColor = DefaultButtonHighlightColor;
        [SerializeField] private Color buttonPressedColor = DefaultButtonPressedColor;

        [Header("Text")]
        [SerializeField] private string titleText = "GAME OVER";
        [SerializeField] private string scoreLabelText = "SCORE";
        [SerializeField] private string restartLabelText = "RESTART";

        [Header("Run Clear Text")]
        [SerializeField] private string runClearTitleText = "RUN CLEAR";
        [SerializeField] private string runClearValueLabelText = "GOLD / TIME";
        [SerializeField] private string runClearRestartLabelText = "RESTART";

        private GameObject _panel;
        private Text _titleValueText;
        private Text _valueLabelText;
        private Text _valueText;
        private Button _primaryActionButton;
        private Text _primaryActionText;
        private bool _isBuilt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            if (FindObjectOfType<GameManager>() == null) {
                return;
            }

            EnsureInstance();
        }

        public static void ShowGameOver(int score) {
            ShowGameOver(score, SceneManager.GetActiveScene().name);
        }

        public static void ShowGameOver(int score, string restartSceneName) {
            Debug.Log($"Showing Game Over UI with score: {score}");

            UIManager manager = UIManager.EnsureInstance();
            if (manager != null && manager.TryShowGameOver(score, restartSceneName)) {
                return;
            }

            GameOverUI ui = EnsureInstance();
            ui.ShowGameOverScreen(score, restartSceneName);
        }

        public static void ShowResult(ResultViewModel result) {
            if (result == null) {
                Debug.LogWarning("GameOverUI.ShowResult called without result data.");
                return;
            }

            UIManager manager = UIManager.EnsureInstance();
            if (manager != null && manager.TryShowResult(result)) {
                return;
            }

            GameOverUI ui = EnsureInstance();
            ui.Show(result);
        }

        public static void ShowRunClear(int gold, float elapsedSeconds, string restartSceneName) {
            UIManager manager = UIManager.EnsureInstance();
            if (manager != null && manager.TryShowRunClear(gold, elapsedSeconds, restartSceneName)) {
                return;
            }

            GameOverUI ui = EnsureInstance();
            ui.ShowRunClearScreen(gold, elapsedSeconds, restartSceneName);
        }

        public static ResultViewModel CreateRunClearResultModel(int gold, float elapsedSeconds, string restartSceneName) {
            GameOverUI ui = EnsureInstance();
            if (ui != null) {
                return ui.CreateConfiguredRunClearResultModel(gold, elapsedSeconds, restartSceneName);
            }

            return new ResultViewModel {
                Preset = ResultPreset.RunClear,
                Title = "RUN CLEAR",
                ValueLabel = "GOLD / TIME",
                ValueText = $"{gold}\n{elapsedSeconds:0.0}s",
                PrimaryActionLabel = "RESTART",
                PrimaryAction = CreateRestartAction(restartSceneName)
            };
        }

        public static ResultViewModel CreateGameOverResultModel(int score, string restartSceneName) {
            GameOverUI ui = EnsureInstance();
            if (ui != null) {
                return ui.CreateConfiguredGameOverResultModel(score, restartSceneName);
            }

            return new ResultViewModel {
                Preset = ResultPreset.GameOver,
                Title = "GAME OVER",
                ValueLabel = "SCORE",
                ValueText = score.ToString(),
                PrimaryActionLabel = "RESTART",
                PrimaryAction = CreateRestartAction(restartSceneName)
            };
        }

        private static GameOverUI EnsureInstance() {
            if (Instance != null) {
                return Instance;
            }

            GameOverUI existing = FindObjectOfType<GameOverUI>();
            if (existing != null) {
                Instance = existing;
                return existing;
            }

            GameObject root = new GameObject("GameOverUI");
            return root.AddComponent<GameOverUI>();
        }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildUI();
            Hide();
        }

        private void OnDestroy() {
            if (Instance == this) {
                Instance = null;
            }
        }

        private void BuildUI() {
            if (_isBuilt) {
                return;
            }

            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DefaultSortingOrder;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = DefaultScaleFactor;
            scaler.referencePixelsPerUnit = 1f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null) {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            RectTransform rootRect = gameObject.GetComponent<RectTransform>();
            if (rootRect == null) {
                rootRect = gameObject.AddComponent<RectTransform>();
            }
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;

            _panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;

            Image panelImage = _panel.GetComponent<Image>();
            panelImage.color = overlayColor;

            Font fontToUse = defaultFont != null ? defaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _titleValueText = CreateText("Title", _panel.transform, fontToUse, titleText, 20, FontStyle.Bold);
            RectTransform titleRect = _titleValueText.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(280f, 32f);
            titleRect.anchoredPosition = new Vector2(0f, 52f);

            _valueLabelText = CreateText("ValueLabel", _panel.transform, fontToUse, scoreLabelText, 12, FontStyle.Normal);
            RectTransform valueLabelRect = _valueLabelText.GetComponent<RectTransform>();
            valueLabelRect.sizeDelta = new Vector2(200f, 20f);
            valueLabelRect.anchoredPosition = new Vector2(0f, 15f);

            _valueText = CreateText("ValueText", _panel.transform, fontToUse, "0", 16, FontStyle.Bold);
            RectTransform valueRect = _valueText.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(200f, 24f);
            valueRect.anchoredPosition = new Vector2(0f, -5f);

            _primaryActionButton = CreateButton("PrimaryActionButton", _panel.transform, fontToUse, restartLabelText, out _primaryActionText);
            RectTransform buttonRect = _primaryActionButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(140f, 26f);
            buttonRect.anchoredPosition = new Vector2(0f, -55f);

            _isBuilt = true;
        }

        private Text CreateText(string name, Transform parent, Font font, string text, int fontSize, FontStyle style) {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            Text textComponent = go.GetComponent<Text>();
            textComponent.font = font;
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            return textComponent;
        }

        private Button CreateButton(string name, Transform parent, Font font, string label, out Text labelText) {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = buttonColor;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHighlightColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHighlightColor;
            button.colors = colors;

            labelText = CreateText("Label", go.transform, font, label, 14, FontStyle.Bold);
            labelText.raycastTarget = false;

            RectTransform labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            return button;
        }

        public void Show(ResultViewModel result) {
            if (!_isBuilt) {
                BuildUI();
            }

            ApplyResult(result);

            if (_panel != null) {
                _panel.SetActive(true);
            }

            EnsureEventSystem();

            if (_primaryActionButton != null) {
                _primaryActionButton.Select();
            }
        }

        public void ShowGameOverScreen(int score) {
            ShowGameOverScreen(score, SceneManager.GetActiveScene().name);
        }

        public void ShowGameOverScreen(int score, string restartSceneName) {
            Show(CreateConfiguredGameOverResultModel(score, restartSceneName));
        }

        public void ShowRunClearScreen(int gold, float elapsedSeconds, string restartSceneName) {
            Show(CreateConfiguredRunClearResultModel(gold, elapsedSeconds, restartSceneName));
        }

        public void HideResult() {
            Hide();
        }

        private void Hide() {
            if (_panel != null) {
                _panel.SetActive(false);
            }
        }

        private void Restart() {
            Scene activeScene = SceneManager.GetActiveScene();
            UIManager.Instance?.ResetGameplayUI();
            CreateRestartAction(activeScene.name)?.Invoke();
        }

        private void ApplyResult(ResultViewModel result) {
            if (result == null) {
                result = CreateConfiguredGameOverResultModel(0, SceneManager.GetActiveScene().name);
            }

            if (_titleValueText != null) {
                _titleValueText.text = string.IsNullOrEmpty(result.Title) ? GetDefaultTitle(result.Preset) : result.Title;
            }

            if (_valueLabelText != null) {
                _valueLabelText.text = string.IsNullOrEmpty(result.ValueLabel) ? GetDefaultValueLabel(result.Preset) : result.ValueLabel;
            }

            if (_valueText != null) {
                _valueText.text = string.IsNullOrEmpty(result.ValueText) ? "0" : result.ValueText;
            }

            if (_primaryActionText != null) {
                _primaryActionText.text = string.IsNullOrEmpty(result.PrimaryActionLabel) ? GetDefaultPrimaryActionLabel(result.Preset) : result.PrimaryActionLabel;
            }

            if (_primaryActionButton != null) {
                _primaryActionButton.onClick.RemoveAllListeners();
                _primaryActionButton.onClick.AddListener(result.PrimaryAction ?? Restart);
            }
        }

        private static UnityAction CreateRestartAction(string sceneName) {
            return () => {
                if (RunManager.Instance != null) {
                    RunManager.Instance.RestartRun(sceneName);
                    return;
                }

                Scene activeScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(activeScene.buildIndex);
            };
        }

        private ResultViewModel CreateConfiguredGameOverResultModel(int score, string restartSceneName) {
            return new ResultViewModel {
                Preset = ResultPreset.GameOver,
                Title = titleText,
                ValueLabel = scoreLabelText,
                ValueText = score.ToString(),
                PrimaryActionLabel = restartLabelText,
                PrimaryAction = CreateRestartAction(restartSceneName)
            };
        }

        private ResultViewModel CreateConfiguredRunClearResultModel(int gold, float elapsedSeconds, string restartSceneName) {
            return new ResultViewModel {
                Preset = ResultPreset.RunClear,
                Title = runClearTitleText,
                ValueLabel = runClearValueLabelText,
                ValueText = $"{gold}\n{elapsedSeconds:0.0}s",
                PrimaryActionLabel = runClearRestartLabelText,
                PrimaryAction = CreateRestartAction(restartSceneName)
            };
        }

        private string GetDefaultTitle(ResultPreset preset) {
            switch (preset) {
                case ResultPreset.RunClear:
                    return runClearTitleText;
                case ResultPreset.GameOver:
                case ResultPreset.Custom:
                default:
                    return titleText;
            }
        }

        private string GetDefaultValueLabel(ResultPreset preset) {
            switch (preset) {
                case ResultPreset.RunClear:
                    return runClearValueLabelText;
                case ResultPreset.GameOver:
                case ResultPreset.Custom:
                default:
                    return scoreLabelText;
            }
        }

        private string GetDefaultPrimaryActionLabel(ResultPreset preset) {
            switch (preset) {
                case ResultPreset.RunClear:
                    return runClearRestartLabelText;
                case ResultPreset.GameOver:
                case ResultPreset.Custom:
                default:
                    return restartLabelText;
            }
        }

        private static void EnsureEventSystem() {
            if (EventSystem.current != null) {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(null);
        }
    }

}
