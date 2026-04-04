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
        private const string PreferredFontResourcePath = "Fonts/Dongle-Regular";
        private static readonly Color DefaultOverlayColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color DefaultButtonColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        private static readonly Color DefaultButtonHighlightColor = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        private static readonly Color DefaultButtonPressedColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        private static readonly Color DefaultSignPanelColor = new Color(0.82f, 0.74f, 0.6f, 0.96f);
        private static readonly Color DefaultSignBorderColor = new Color(0.22f, 0.16f, 0.08f, 0.95f);
        private static readonly Color DefaultRunClearPanelColor = new Color(0.1f, 0.1f, 0.1f, 0.92f);

        public static GameOverUI Instance { get; private set; }

        [Header("Style")]
        [SerializeField] private Font defaultFont;
        [SerializeField] private Color overlayColor = DefaultOverlayColor;
        [SerializeField] private Color buttonColor = DefaultButtonColor;
        [SerializeField] private Color buttonHighlightColor = DefaultButtonHighlightColor;
        [SerializeField] private Color buttonPressedColor = DefaultButtonPressedColor;

        [Header("Text")]
        [SerializeField] private string titleText = "\uC0AC\uB9DD";
        [SerializeField] private string scoreLabelText = "\uC810\uC218";
        [SerializeField] private string restartLabelText = "\uB2E4\uC2DC \uC2DC\uC791";

        [Header("Run Clear Text")]
        [SerializeField] private string runClearTitleText = "\uD074\uB9AC\uC5B4";
        [SerializeField] private string runClearValueLabelText = "\uD669\uAE08 / \uC2DC\uAC04";
        [SerializeField] private string runClearRestartLabelText = "\uB2E4\uC2DC \uC2DC\uC791";

        private GameObject _panel;
        private Image _contentPanelImage;
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
                Title = "\uD074\uB9AC\uC5B4",
                ValueLabel = "\uD669\uAE08 / \uC2DC\uAC04",
                ValueText = $"{gold}\n{elapsedSeconds:0.0}\uCD08",
                PrimaryActionLabel = "\uB2E4\uC2DC \uC2DC\uC791",
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
                Title = "\uC0AC\uB9DD",
                ValueLabel = "\uC810\uC218",
                ValueText = score.ToString(),
                PrimaryActionLabel = "\uB2E4\uC2DC \uC2DC\uC791",
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

        private void Update() {
            if (_panel == null || !_panel.activeInHierarchy || _primaryActionButton == null || !_primaryActionButton.gameObject.activeInHierarchy) {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
                _primaryActionButton.onClick.Invoke();
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

            Font fontToUse = GetPreferredFont();

            GameObject contentPanel = new GameObject("ResultCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            contentPanel.transform.SetParent(_panel.transform, false);

            RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 8f);
            contentRect.sizeDelta = new Vector2(210f, 168f);

            _contentPanelImage = contentPanel.GetComponent<Image>();
            _contentPanelImage.color = DefaultRunClearPanelColor;

            Outline cardOutline = contentPanel.GetComponent<Outline>();
            cardOutline.effectColor = DefaultSignBorderColor;
            cardOutline.effectDistance = new Vector2(2f, -2f);
            cardOutline.useGraphicAlpha = true;

            _titleValueText = CreateText("Title", contentPanel.transform, fontToUse, titleText, 20, FontStyle.Bold);
            RectTransform titleRect = _titleValueText.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(180f, 36f);
            titleRect.anchoredPosition = new Vector2(0f, 50f);

            _valueLabelText = CreateText("ValueLabel", contentPanel.transform, fontToUse, scoreLabelText, 12, FontStyle.Normal);
            RectTransform valueLabelRect = _valueLabelText.GetComponent<RectTransform>();
            valueLabelRect.sizeDelta = new Vector2(180f, 28f);
            valueLabelRect.anchoredPosition = new Vector2(0f, 16f);

            _valueText = CreateText("ValueText", contentPanel.transform, fontToUse, "0", 16, FontStyle.Bold);
            RectTransform valueRect = _valueText.GetComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(180f, 56f);
            valueRect.anchoredPosition = new Vector2(0f, -12f);

            _primaryActionButton = CreateButton("PrimaryActionButton", contentPanel.transform, fontToUse, restartLabelText, out _primaryActionText);
            RectTransform buttonRect = _primaryActionButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(140f, 26f);
            buttonRect.anchoredPosition = new Vector2(0f, -64f);

            _isBuilt = true;
        }

        private Font GetPreferredFont() {
            Font preferredFont = Resources.Load<Font>(PreferredFontResourcePath);
            if (preferredFont != null) {
                return preferredFont;
            }

            if (defaultFont != null) {
                return defaultFont;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;

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

            ApplyVisualPreset(result.Preset);
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
                ValueText = $"{gold}\n{elapsedSeconds:0.0}\uCD08",
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

        private void ApplyVisualPreset(ResultPreset preset) {
            if (_contentPanelImage == null || _titleValueText == null || _valueLabelText == null || _valueText == null || _primaryActionText == null) {
                return;
            }

            RectTransform contentRect = _contentPanelImage.rectTransform;
            RectTransform titleRect = _titleValueText.rectTransform;
            RectTransform labelRect = _valueLabelText.rectTransform;
            RectTransform valueRect = _valueText.rectTransform;
            RectTransform buttonRect = _primaryActionButton != null ? _primaryActionButton.GetComponent<RectTransform>() : null;

            bool isGameOver = preset == ResultPreset.GameOver;
            _contentPanelImage.color = isGameOver ? DefaultSignPanelColor : DefaultRunClearPanelColor;

            if (contentRect != null) {
                contentRect.sizeDelta = isGameOver ? new Vector2(214f, 228f) : new Vector2(210f, 168f);
                contentRect.anchoredPosition = isGameOver ? new Vector2(0f, 4f) : new Vector2(0f, 8f);
            }

            _titleValueText.color = isGameOver ? DefaultSignBorderColor : Color.white;
            _valueLabelText.color = isGameOver ? DefaultSignBorderColor : Color.white;
            _valueText.color = isGameOver ? DefaultSignBorderColor : Color.white;
            _primaryActionText.color = Color.white;

            _titleValueText.fontSize = isGameOver ? 38 : 20;
            _valueLabelText.fontSize = isGameOver ? 26 : 12;
            _valueText.fontSize = isGameOver ? 24 : 16;
            _primaryActionText.fontSize = isGameOver ? 18 : 14;
            _titleValueText.fontStyle = FontStyle.Bold;
            _valueLabelText.fontStyle = FontStyle.Bold;
            _valueText.fontStyle = isGameOver ? FontStyle.Normal : FontStyle.Bold;
            _primaryActionText.fontStyle = FontStyle.Bold;

            if (titleRect != null) {
                titleRect.sizeDelta = isGameOver ? new Vector2(172f, 48f) : new Vector2(180f, 36f);
                titleRect.anchoredPosition = isGameOver ? new Vector2(0f, 78f) : new Vector2(0f, 50f);
            }

            if (labelRect != null) {
                labelRect.sizeDelta = isGameOver ? new Vector2(172f, 38f) : new Vector2(180f, 28f);
                labelRect.anchoredPosition = isGameOver ? new Vector2(0f, 38f) : new Vector2(0f, 16f);
            }

            if (valueRect != null) {
                valueRect.sizeDelta = isGameOver ? new Vector2(172f, 84f) : new Vector2(180f, 56f);
                valueRect.anchoredPosition = isGameOver ? new Vector2(0f, -18f) : new Vector2(0f, -12f);
            }

            if (buttonRect != null) {
                buttonRect.sizeDelta = isGameOver ? new Vector2(116f, 28f) : new Vector2(140f, 26f);
                buttonRect.anchoredPosition = isGameOver ? new Vector2(0f, -90f) : new Vector2(0f, -64f);
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
