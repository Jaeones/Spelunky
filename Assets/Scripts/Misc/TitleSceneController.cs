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
        [SerializeField] private string quitButtonText = "QUIT";
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.07f, 0.05f, 1f);
        [SerializeField] private string existingPlayTextObjectName = "Play";
        [SerializeField] private string existingQuitTextObjectName = "Quit";

        private Button _playButton;
        private Button _quitButton;

        private void Awake() {
            EnsureCamera();
            EnsureEventSystem();

            if (!TryBindExistingMenu()) {
                CreateTitleUI();
            }
        }

        private void Update() {
            HandleKeyboardNavigation();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {
                ActivateCurrentSelection();
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
            Text quitText = FindTextByName(existingQuitTextObjectName);

            if (playText == null || quitText == null) {
                return false;
            }

            _playButton = BindTextAsButton(playText, StartGame);
            _quitButton = BindTextAsButton(quitText, QuitGame);
            LinkNavigation(_playButton, _quitButton);
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

            CreateFullscreenPanel(canvasObject.transform, backgroundColor, "Backdrop");
            CreateTitleBlock(canvasObject.transform, font);
            CreateButtonStack(canvasObject.transform, font);
        }

        private void CreateTitleBlock(Transform parent, Font font) {
            CreateText(parent, "Title", titleText, font, 34, FontStyle.Bold, new Vector2(0.5f, 0.72f), new Vector2(0f, 0f), new Vector2(320f, 48f), TextAnchor.MiddleCenter, Color.white);
            CreateText(parent, "Subtitle", subtitleText, font, 12, FontStyle.Normal, new Vector2(0.5f, 0.60f), new Vector2(0f, 0f), new Vector2(260f, 24f), TextAnchor.MiddleCenter, new Color(0.87f, 0.78f, 0.63f, 1f));
            CreateText(parent, "Hint", "ENTER / SPACE", font, 10, FontStyle.Normal, new Vector2(0.5f, 0.18f), new Vector2(0f, 0f), new Vector2(180f, 20f), TextAnchor.MiddleCenter, new Color(0.76f, 0.74f, 0.68f, 1f));
        }

        private void CreateButtonStack(Transform parent, Font font) {
            GameObject startButton = CreateButton(parent, "StartButton", startButtonText, font, new Vector2(0.5f, 0.42f), new Vector2(180f, 32f), new Color(0.74f, 0.60f, 0.28f, 1f));
            startButton.GetComponent<Button>().onClick.AddListener(StartGame);

            GameObject quitButton = CreateButton(parent, "QuitButton", quitButtonText, font, new Vector2(0.5f, 0.31f), new Vector2(180f, 28f), new Color(0.28f, 0.23f, 0.18f, 1f));
            quitButton.GetComponent<Button>().onClick.AddListener(QuitGame);
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

        private static void LinkNavigation(Button playButton, Button quitButton) {
            if (playButton == null || quitButton == null) {
                return;
            }

            Navigation playNavigation = playButton.navigation;
            playNavigation.mode = Navigation.Mode.Explicit;
            playNavigation.selectOnDown = quitButton;
            playNavigation.selectOnUp = quitButton;
            playButton.navigation = playNavigation;

            Navigation quitNavigation = quitButton.navigation;
            quitNavigation.mode = Navigation.Mode.Explicit;
            quitNavigation.selectOnDown = playButton;
            quitNavigation.selectOnUp = playButton;
            quitButton.navigation = quitNavigation;
        }

        private void HandleKeyboardNavigation() {
            if (EventSystem.current == null || _playButton == null || _quitButton == null) {
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
                EventSystem.current.SetSelectedGameObject(movedDown ? _quitButton.gameObject : _playButton.gameObject);
                return;
            }

            if (currentSelection == _playButton.gameObject && (movedDown || movedByTab)) {
                EventSystem.current.SetSelectedGameObject(_quitButton.gameObject);
                return;
            }

            if (currentSelection == _quitButton.gameObject && (movedUp || movedByTab)) {
                EventSystem.current.SetSelectedGameObject(_playButton.gameObject);
            }
        }

        private void ActivateCurrentSelection() {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) {
                StartGame();
                return;
            }

            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
            if (currentSelection == _quitButton?.gameObject) {
                QuitGame();
                return;
            }

            StartGame();
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
