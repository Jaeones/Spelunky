using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Spelunky {

    /// <summary>
    /// Temporary title scene controller.
    /// Creates a minimal runtime menu so the project can start from Title before
    /// a fuller Boot/Menu flow is implemented.
    /// </summary>
    public class TitleSceneController : MonoBehaviour {

        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private string titleText = "SPELUNKY";
        [SerializeField] private string subtitleText = "4-stage prototype";
        [SerializeField] private string startButtonText = "START GAME";
        [SerializeField] private string settingsButtonText = "SETTINGS";
        [SerializeField] private string quitButtonText = "QUIT";
        [SerializeField] private string settingsTitleText = "SOUND";
        [SerializeField] private string masterVolumeLabelText = "MASTER";
        [SerializeField] private string backgroundVolumeLabelText = "BACKGROUND";
        [SerializeField] private string sfxVolumeLabelText = "SFX";
        [SerializeField] private string closeButtonText = "CLOSE";
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.07f, 0.05f, 1f);
        [SerializeField] private string existingPlayTextObjectName = "Play";
        [SerializeField] private string existingSettingTextObjectName = "Setting";
        [SerializeField] private string existingQuitTextObjectName = "Quit";
        [SerializeField] private Texture2D cursorTexture;
        [SerializeField] private Vector2 cursorHotspot = new Vector2(282f, 232f);
        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

        private Button _playButton;
        private Button _quitButton;
        private Button _settingsButton;
        private SettingUI _settingUI;
        private Transform _menuCanvasRoot;
        private AudioSource _titleAudioSource;

        private void Awake() {
            _titleAudioSource = GetComponent<AudioSource>();
            EnsureCamera();
            EnsureEventSystem();

            if (!TryBindExistingMenu()) {
                CreateTitleUI();
            }

            BindSettingsUI();
            ApplyTitleAudioSettings();
            ApplyCursor();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Escape) && IsSettingsOpen()) {
                SetSettingsPanelVisible(false);
                return;
            }

            if (IsSettingsOpen()) {
                return;
            }

            HandleKeyboardNavigation();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {
                ActivateCurrentSelection();
            }
        }

        private void OnDestroy() {
            if (_settingUI != null) {
                _settingUI.VolumesChanged -= ApplyTitleAudioSettings;
            }
        }

        public void StartGame() {
            RunManager.EnsureInstance().StartFreshRun();
            SceneManager.LoadScene(gameSceneName);
        }

        public void QuitGame() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ToggleSettings() {
            SetSettingsPanelVisible(!IsSettingsOpen());
        }

        private static void EnsureEventSystem() {
            if (FindObjectOfType<EventSystem>() != null) {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void EnsureCamera() {
            if (Camera.main != null || FindObjectOfType<Camera>() != null) {
                return;
            }

            GameObject cameraObject = new GameObject("TitleCamera");
            Camera cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = backgroundColor;
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 5f;
            cameraComponent.nearClipPlane = 0.3f;
            cameraComponent.farClipPlane = 1000f;
            cameraObject.tag = "MainCamera";

            AudioListener audioListener = cameraObject.AddComponent<AudioListener>();
            audioListener.enabled = true;
        }

        private bool TryBindExistingMenu() {
            Text playText = FindTextByName(existingPlayTextObjectName);
            Text settingText = FindTextByName(existingSettingTextObjectName);
            Text quitText = FindTextByName(existingQuitTextObjectName);

            if (playText == null || settingText == null || quitText == null) {
                return false;
            }

            _playButton = BindTextAsButton(playText, StartGame);
            _settingsButton = BindTextAsButton(settingText, ToggleSettings);
            _quitButton = BindTextAsButton(quitText, QuitGame);
            LinkNavigation(_playButton, _settingsButton, _quitButton);
            _menuCanvasRoot = playText.canvas != null ? playText.canvas.transform : playText.transform.root;
            return true;
        }

        private void CreateTitleUI() {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new GameObject("TitleCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(320f, 180f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;
            _menuCanvasRoot = canvasObject.transform;

            CreateFullscreenPanel(canvasObject.transform, backgroundColor, "Backdrop");
            CreateTitleBlock(canvasObject.transform, font);
            CreateButtonStack(canvasObject.transform, font);
        }

        private void BindSettingsUI() {
            SettingUI[] existingSettingUIs = FindObjectsOfType<SettingUI>(true);
            _settingUI = existingSettingUIs.Length > 0 ? existingSettingUIs[0] : null;
            if (_settingUI == null) {
                Transform root = FindSettingUIRoot();
                if (root != null) {
                    _settingUI = root.GetComponent<SettingUI>();
                    if (_settingUI == null) {
                        _settingUI = root.gameObject.AddComponent<SettingUI>();
                    }
                }
            }

            if (_settingUI == null) {
                return;
            }

            _settingUI.VolumesChanged -= ApplyTitleAudioSettings;
            _settingUI.VolumesChanged += ApplyTitleAudioSettings;
            _settingUI.SetVisible(false);
        }

        private GameObject CreateSettingsPanel(Transform parent, Font font) {
            GameObject panelObject = new GameObject("SettingsPanel");
            panelObject.transform.SetParent(parent, false);

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(240f, 136f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.10f, 0.09f, 0.07f, 0.96f);

            CreateText(panelObject.transform, "SettingsTitle", settingsTitleText, font, 16, FontStyle.Bold, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(180f, 24f), TextAnchor.MiddleCenter, Color.white);

            CreateVolumeSliderRow(panelObject.transform, font, masterVolumeLabelText, new Vector2(0.5f, 0.66f), AudioManager.GetSavedMasterVolume(), SetMasterVolume);
            CreateVolumeSliderRow(panelObject.transform, font, backgroundVolumeLabelText, new Vector2(0.5f, 0.48f), AudioManager.GetSavedBackgroundVolume(), SetBackgroundVolume);
            CreateVolumeSliderRow(panelObject.transform, font, sfxVolumeLabelText, new Vector2(0.5f, 0.30f), AudioManager.GetSavedSfxVolume(), SetSfxVolume);

            Button closeButton = CreateTextButton(panelObject.transform, "CloseButton", closeButtonText, font, new Vector2(0.5f, 0.11f), new Vector2(92f, 24f), new Color(0.40f, 0.30f, 0.18f, 1f));
            closeButton.onClick.AddListener(() => SetSettingsPanelVisible(false));

            return panelObject;
        }

        private void CreateVolumeSliderRow(Transform parent, Font font, string label, Vector2 anchor, float initialValue, UnityEngine.Events.UnityAction<float> onValueChanged) {
            CreateText(parent, label + "Label", label, font, 11, FontStyle.Bold, anchor + new Vector2(-0.30f, 0f), Vector2.zero, new Vector2(90f, 18f), TextAnchor.MiddleLeft, new Color(0.92f, 0.86f, 0.72f, 1f));

            Text valueText = CreateText(parent, label + "Value", FormatVolumeValue(initialValue), font, 10, FontStyle.Normal, anchor + new Vector2(0.33f, 0f), Vector2.zero, new Vector2(36f, 18f), TextAnchor.MiddleRight, Color.white);
            Slider slider = CreateSlider(parent, label + "Slider", anchor, new Vector2(120f, 16f));
            slider.value = initialValue;
            slider.onValueChanged.AddListener(value => {
                valueText.text = FormatVolumeValue(value);
                onValueChanged.Invoke(value);
            });
        }

        private void CreateTitleBlock(Transform parent, Font font) {
            CreateText(parent, "Title", titleText, font, 34, FontStyle.Bold, new Vector2(0.5f, 0.72f), new Vector2(0f, 0f), new Vector2(320f, 48f), TextAnchor.MiddleCenter, Color.white);
            CreateText(parent, "Subtitle", subtitleText, font, 12, FontStyle.Normal, new Vector2(0.5f, 0.60f), new Vector2(0f, 0f), new Vector2(260f, 24f), TextAnchor.MiddleCenter, new Color(0.87f, 0.78f, 0.63f, 1f));
            CreateText(parent, "Hint", "ENTER / SPACE", font, 10, FontStyle.Normal, new Vector2(0.5f, 0.18f), new Vector2(0f, 0f), new Vector2(180f, 20f), TextAnchor.MiddleCenter, new Color(0.76f, 0.74f, 0.68f, 1f));
        }

        private void CreateButtonStack(Transform parent, Font font) {
            GameObject startButton = CreateButton(parent, "StartButton", startButtonText, font, new Vector2(0.5f, 0.42f), new Vector2(180f, 32f), new Color(0.74f, 0.60f, 0.28f, 1f));
            _playButton = startButton.GetComponent<Button>();
            _playButton.onClick.AddListener(StartGame);

            GameObject settingsButton = CreateButton(parent, "SettingsButton", settingsButtonText, font, new Vector2(0.5f, 0.31f), new Vector2(180f, 28f), new Color(0.41f, 0.33f, 0.20f, 1f));
            _settingsButton = settingsButton.GetComponent<Button>();
            _settingsButton.onClick.AddListener(ToggleSettings);

            GameObject quitButton = CreateButton(parent, "QuitButton", quitButtonText, font, new Vector2(0.5f, 0.20f), new Vector2(180f, 28f), new Color(0.28f, 0.23f, 0.18f, 1f));
            _quitButton = quitButton.GetComponent<Button>();
            _quitButton.onClick.AddListener(QuitGame);

            LinkNavigation(_playButton, _settingsButton, _quitButton);
        }

        private static GameObject CreateFullscreenPanel(Transform parent, Color color, string name) {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            return panelObject;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Font font, Vector2 anchor, Vector2 size, Color color) {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = new Color(color.r + 0.08f, color.g + 0.08f, color.b + 0.08f, 1f);
            colors.pressedColor = new Color(color.r * 0.85f, color.g * 0.85f, color.b * 0.85f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            CreateText(buttonObject.transform, "Label", label, font, 12, FontStyle.Bold, new Vector2(0.5f, 0.5f), Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
            return buttonObject;
        }

        private static Button CreateTextButton(Transform parent, string name, string label, Font font, Vector2 anchor, Vector2 size, Color color) {
            return CreateButton(parent, name, label, font, anchor, size, color).GetComponent<Button>();
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchor, Vector2 size) {
            GameObject sliderObject = new GameObject(name);
            sliderObject.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
            sliderRect.anchorMin = anchor;
            sliderRect.anchorMax = anchor;
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = size;

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(sliderObject.transform, false);
            RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.20f, 0.18f, 0.14f, 1f);

            GameObject fillAreaObject = new GameObject("Fill Area");
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-6f, 0f);

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.15f);
            fillRect.anchorMax = new Vector2(1f, 0.85f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.78f, 0.64f, 0.30f, 1f);

            GameObject handleAreaObject = new GameObject("Handle Slide Area");
            handleAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleAreaObject.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(6f, 0f);
            handleAreaRect.offsetMax = new Vector2(-6f, 0f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(handleAreaObject.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(10f, 18f);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.targetGraphic = handleImage;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;

            return slider;
        }

        private static Text FindTextByName(string objectName) {
            if (string.IsNullOrWhiteSpace(objectName)) {
                return null;
            }

            Text[] texts = FindObjectsOfType<Text>(true);
            for (int i = 0; i < texts.Length; i++) {
                Text text = texts[i];
                if (text != null && text.name == objectName) {
                    return text;
                }
            }

            return null;
        }

        private static Button BindTextAsButton(Text text, UnityEngine.Events.UnityAction action) {
            if (text == null || action == null) {
                return null;
            }

            Button button = text.GetComponent<Button>();
            if (button == null) {
                button = text.gameObject.AddComponent<Button>();
            }

            if (text.GetComponent<TitleMenuTextEffect>() == null) {
                text.gameObject.AddComponent<TitleMenuTextEffect>();
            }

            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = text;

            Color baseColor = text.color;
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = new Color(
                Mathf.Min(1f, baseColor.r + 0.12f),
                Mathf.Min(1f, baseColor.g + 0.12f),
                Mathf.Min(1f, baseColor.b + 0.12f),
                baseColor.a
            );
            colors.pressedColor = new Color(baseColor.r * 0.85f, baseColor.g * 0.85f, baseColor.b * 0.85f, baseColor.a);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            text.raycastTarget = true;
            return button;
        }

        private static void LinkNavigation(Button playButton, Button settingsButton, Button quitButton) {
            if (playButton == null || settingsButton == null || quitButton == null) {
                return;
            }

            Navigation playNavigation = playButton.navigation;
            playNavigation.mode = Navigation.Mode.Explicit;
            playNavigation.selectOnDown = settingsButton;
            playNavigation.selectOnUp = quitButton;
            playButton.navigation = playNavigation;

            Navigation settingsNavigation = settingsButton.navigation;
            settingsNavigation.mode = Navigation.Mode.Explicit;
            settingsNavigation.selectOnDown = quitButton;
            settingsNavigation.selectOnUp = playButton;
            settingsButton.navigation = settingsNavigation;

            Navigation quitNavigation = quitButton.navigation;
            quitNavigation.mode = Navigation.Mode.Explicit;
            quitNavigation.selectOnDown = playButton;
            quitNavigation.selectOnUp = settingsButton;
            quitButton.navigation = quitNavigation;
        }

        private void HandleKeyboardNavigation() {
            if (EventSystem.current == null || _playButton == null || _settingsButton == null || _quitButton == null) {
                return;
            }

            bool movedUp = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            bool movedDown = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            bool movedByTab = Input.GetKeyDown(KeyCode.Tab);

            if (!movedUp && !movedDown && !movedByTab) {
                return;
            }

            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

            if (currentSelection == null) {
                EventSystem.current.SetSelectedGameObject(movedDown ? _settingsButton.gameObject : _playButton.gameObject);
                return;
            }

            if (currentSelection == _playButton.gameObject && (movedDown || movedByTab)) {
                EventSystem.current.SetSelectedGameObject(_settingsButton.gameObject);
                return;
            }

            if (currentSelection == _settingsButton.gameObject && movedDown) {
                EventSystem.current.SetSelectedGameObject(_quitButton.gameObject);
                return;
            }

            if (currentSelection == _settingsButton.gameObject && movedUp) {
                EventSystem.current.SetSelectedGameObject(_playButton.gameObject);
                return;
            }

            if (currentSelection == _settingsButton.gameObject && movedByTab) {
                EventSystem.current.SetSelectedGameObject(_quitButton.gameObject);
                return;
            }

            if (currentSelection == _quitButton.gameObject && (movedUp || movedByTab)) {
                EventSystem.current.SetSelectedGameObject(_settingsButton.gameObject);
                return;
            }

            if (currentSelection == _quitButton.gameObject && movedDown) {
                EventSystem.current.SetSelectedGameObject(_playButton.gameObject);
            }
        }

        private void ActivateCurrentSelection() {
            if (IsSettingsOpen()) {
                return;
            }

            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) {
                StartGame();
                return;
            }

            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
            if (currentSelection == _settingsButton?.gameObject) {
                ToggleSettings();
                return;
            }

            if (currentSelection == _quitButton?.gameObject) {
                QuitGame();
                return;
            }

            StartGame();
        }

        private bool IsSettingsOpen() {
            return _settingUI != null && _settingUI.IsVisible;
        }

        private void SetMasterVolume(float value) {
            AudioManager.SetSavedMasterVolume(value);
            ApplyTitleAudioSettings();
        }

        private void SetBackgroundVolume(float value) {
            AudioManager.SetSavedBackgroundVolume(value);
            ApplyTitleAudioSettings();
        }

        private void SetSfxVolume(float value) {
            AudioManager.SetSavedSfxVolume(value);
            ApplyTitleAudioSettings();
        }

        private void SetSettingsPanelVisible(bool isVisible) {
            if (_settingUI == null) {
                return;
            }

            _settingUI.SetVisible(isVisible);

            if (!isVisible) {
                EventSystem.current?.SetSelectedGameObject(_settingsButton != null ? _settingsButton.gameObject : _playButton?.gameObject);
            }
        }

        private Transform FindSettingUIRoot() {
            if (_menuCanvasRoot != null) {
                RectTransform[] rectTransforms = _menuCanvasRoot.GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rectTransforms.Length; i++) {
                    if (rectTransforms[i] != null && rectTransforms[i].name == "SettingUI") {
                        return rectTransforms[i];
                    }
                }
            }

            RectTransform[] allRects = FindObjectsOfType<RectTransform>(true);
            for (int i = 0; i < allRects.Length; i++) {
                if (allRects[i] != null && allRects[i].name == "SettingUI") {
                    return allRects[i];
                }
            }

            return null;
        }

        private static string FormatVolumeValue(float value) {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private void ApplyTitleAudioSettings() {
            AudioListener.volume = AudioManager.GetSavedMasterVolume();

            if (_titleAudioSource != null) {
                _titleAudioSource.volume = AudioManager.GetSavedBackgroundVolume();
            }
        }

        private void ApplyCursor() {
            if (cursorTexture == null) {
                return;
            }

            Cursor.SetCursor(cursorTexture, cursorHotspot, cursorMode);
            Cursor.visible = true;
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, int fontSize, FontStyle fontStyle, Vector2 anchor, Vector2 position, Vector2 size, TextAnchor alignment, Color color) {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

    }

}
