using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Spelunky {

    public class EndingSceneController : MonoBehaviour {

        private const string DefaultHeadlineText = "ENDING";
        private const string DefaultIntroText = "\uD669\uAE08 \uC6B0\uC0C1\uC744 \uD488\uACE0, \uB05D\uB0B4 \uC9C0\uC0C1\uC73C\uB85C \uB3CC\uC544\uC654\uB2E4.";
        private const string DefaultOutroText = "\uADF8\uB7EC\uB098 \uB3D9\uAD74\uC758 \uC5B4\uB460\uC740 \uC5B8\uC81C\uB098 \uB2E4\uC74C \uD0D0\uD5D8\uAC00\uB97C \uAE30\uB2E4\uB9B0\uB2E4.";
        private const string DefaultContinueText = "\uC544\uBB34 \uD0A4\uB098 \uB20C\uB7EC \uD0C0\uC774\uD2C0\uB85C";

        [SerializeField] private string titleSceneName = "Title";
        [SerializeField] private Sprite endingSprite;
        [SerializeField] private Font endingFont;
        [SerializeField] private string headlineText = DefaultHeadlineText;
        [SerializeField] private string introText = DefaultIntroText;
        [SerializeField] private string outroText = DefaultOutroText;
        [SerializeField] private string continueText = DefaultContinueText;
        [SerializeField] private Color backgroundColor = new Color(0.04f, 0.03f, 0.02f, 1f);
        [SerializeField] private Color imageTint = Color.white;
        [SerializeField] private Color textColor = new Color(0.95f, 0.9f, 0.8f, 1f);
        [SerializeField] private Color promptColor = new Color(0.93f, 0.81f, 0.56f, 1f);
        [SerializeField] private float imageFadeDuration = 1.6f;
        [SerializeField] private float textFadeDelay = 0.9f;
        [SerializeField] private float textFadeDuration = 1.1f;
        [SerializeField] private float typewriterDuration = 3f;
        [SerializeField] private float inputGraceSeconds = 0.35f;
        [SerializeField] private float promptBlinkSpeed = 1.2f;
        [SerializeField] private Vector2 endingImageSize = new Vector2(320f, 210f);
        [SerializeField] private Vector2 continuePromptAnchor = new Vector2(0.5f, 0.03f);
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.9f);
        [SerializeField] private Vector2 outlineDistance = new Vector2(1.5f, -1.5f);

        private Image _endingImage;
        private Text _headlineLabel;
        private Text _bodyLabel;
        private Text _promptLabel;
        private float _elapsed;
        private string _bodyFullText;

        private void Awake() {
            HideCursor();
            EnsureCamera();
            BuildUi();
        }

        private void Update() {
            _elapsed += Time.deltaTime;
            UpdatePresentation();

            if (_elapsed >= GetPromptStartTime() + inputGraceSeconds && Input.anyKeyDown) {
                SceneManager.LoadScene(titleSceneName);
            }
        }

        private void EnsureCamera() {
            if (Camera.main != null || FindObjectOfType<Camera>() != null) {
                return;
            }

            GameObject cameraObject = new GameObject("EndingCamera");
            Camera cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = backgroundColor;
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 5f;
            cameraComponent.nearClipPlane = 0.3f;
            cameraComponent.farClipPlane = 1000f;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        private static void HideCursor() {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void BuildUi() {
            Font fontToUse = endingFont != null ? endingFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (TryBindExistingUi(fontToUse)) {
                _bodyFullText = _bodyLabel != null ? _bodyLabel.text : string.Empty;

                AddOutline(_headlineLabel);
                AddOutline(_bodyLabel);
                AddOutline(_promptLabel);

                SetGraphicAlpha(_endingImage, 0f);
                SetGraphicAlpha(_headlineLabel, 0f);
                SetGraphicAlpha(_bodyLabel, 0f);
                SetGraphicAlpha(_promptLabel, 0f);
                ApplyBodyTypewriter(0f);
                return;
            }

            GameObject canvasObject = new GameObject("EndingCanvas");
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

            _endingImage = CreateImage(
                canvasObject.transform,
                "EndingImage",
                endingSprite,
                new Vector2(0.5f, 0.55f),
                endingImageSize,
                imageTint
            );
            _endingImage.preserveAspect = true;

            _headlineLabel = CreateText(
                canvasObject.transform,
                "Headline",
                ResolveString(headlineText, DefaultHeadlineText),
                fontToUse,
                34,
                FontStyle.Bold,
                new Vector2(0.5f, 0.9f),
                new Vector2(260f, 40f),
                TextAnchor.MiddleCenter,
                textColor
            );
            _bodyLabel = CreateText(
                canvasObject.transform,
                "Body",
                BuildBodyText(),
                fontToUse,
                20,
                FontStyle.Normal,
                new Vector2(0.5f, 0.16f),
                new Vector2(280f, 72f),
                TextAnchor.UpperCenter,
                textColor
            );
            _promptLabel = CreateText(
                canvasObject.transform,
                "Prompt",
                ResolveString(continueText, DefaultContinueText),
                fontToUse,
                18,
                FontStyle.Bold,
                continuePromptAnchor,
                new Vector2(220f, 24f),
                TextAnchor.MiddleCenter,
                promptColor
            );

            AddOutline(_headlineLabel);
            AddOutline(_bodyLabel);
            AddOutline(_promptLabel);

            _bodyFullText = _bodyLabel != null ? _bodyLabel.text : string.Empty;

            SetGraphicAlpha(_endingImage, 0f);
            SetGraphicAlpha(_headlineLabel, 0f);
            SetGraphicAlpha(_bodyLabel, 0f);
            SetGraphicAlpha(_promptLabel, 0f);
            ApplyBodyTypewriter(0f);
        }

        private bool TryBindExistingUi(Font fontToUse) {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) {
                return false;
            }

            _endingImage = FindImageByName(canvas.transform, "BG");
            _headlineLabel = FindTextByName(canvas.transform, "Title");
            _bodyLabel = FindTextByName(canvas.transform, "Text");
            _promptLabel = FindTextByName(canvas.transform, "Prompt")
                ?? FindTextByName(canvas.transform, "Continue")
                ?? FindTextByName(canvas.transform, "ContinueText")
                ?? FindTextByName(canvas.transform, "PressAnyKey");

            if (_endingImage == null || _headlineLabel == null || _bodyLabel == null) {
                return false;
            }

            _endingImage.sprite = endingSprite != null ? endingSprite : _endingImage.sprite;
            _endingImage.color = imageTint;
            _endingImage.preserveAspect = true;

            ConfigureText(
                _headlineLabel,
                fontToUse,
                ResolveString(headlineText, DefaultHeadlineText),
                null,
                null,
                TextAnchor.MiddleCenter,
                textColor
            );

            ConfigureText(
                _bodyLabel,
                fontToUse,
                null,
                null,
                null,
                TextAnchor.UpperCenter,
                textColor
            );

            if (_promptLabel == null) {
                _promptLabel = CreateText(
                    canvas.transform,
                    "Prompt",
                    ResolveString(continueText, DefaultContinueText),
                    fontToUse,
                    18,
                    FontStyle.Bold,
                    continuePromptAnchor,
                    new Vector2(220f, 24f),
                    TextAnchor.MiddleCenter,
                    promptColor
                );
            } else {
                ConfigureText(
                    _promptLabel,
                    fontToUse,
                    ResolveString(continueText, DefaultContinueText),
                    null,
                    null,
                    TextAnchor.MiddleCenter,
                    promptColor
                );
            }

            return true;
        }

        private void UpdatePresentation() {
            float imageAlpha = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, imageFadeDuration));
            float textAlpha = Mathf.Clamp01((_elapsed - textFadeDelay) / Mathf.Max(0.01f, textFadeDuration));
            float typewriterProgress = Mathf.Clamp01((_elapsed - textFadeDelay) / Mathf.Max(0.01f, typewriterDuration));
            float promptAlpha = 0f;
            float promptStartTime = GetPromptStartTime();

            if (_elapsed >= promptStartTime) {
                promptAlpha = 0.55f + Mathf.PingPong((_elapsed - promptStartTime) * promptBlinkSpeed, 0.45f);
            }

            SetGraphicAlpha(_endingImage, imageAlpha);
            SetGraphicAlpha(_headlineLabel, textAlpha);
            SetGraphicAlpha(_bodyLabel, textAlpha);
            SetGraphicAlpha(_promptLabel, promptAlpha);
            ApplyBodyTypewriter(typewriterProgress);
        }

        private float GetPromptStartTime() {
            return textFadeDelay + typewriterDuration;
        }

        private void ApplyBodyTypewriter(float progress) {
            if (_bodyLabel == null || _bodyFullText == null) {
                return;
            }

            int visibleCharacters = Mathf.Clamp(
                Mathf.RoundToInt(_bodyFullText.Length * Mathf.Clamp01(progress)),
                0,
                _bodyFullText.Length
            );
            _bodyLabel.text = _bodyFullText.Substring(0, visibleCharacters);
        }

        private string BuildBodyText() {
            string intro = ResolveString(introText, DefaultIntroText);
            string outro = ResolveString(outroText, DefaultOutroText);

            RunResult result = RunManager.Instance != null ? RunManager.Instance.LastCompletedResult : null;
            if (result == null) {
                return $"{intro}\n\n{outro}";
            }

            return $"{intro}\n\uAE08 {result.finalGold}\uC744 \uC190\uC5D0 \uC950 \uCC44 {result.totalDurationSeconds:0.0}\uCD08\uC758 \uC6D0\uC815\uC744 \uB9C8\uBB34\uB9AC\uD588\uB2E4.\n{outro}";
        }

        private static string ResolveString(string value, string fallback) {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static GameObject CreateFullscreenPanel(Transform parent, Color color, string name) {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            return panelObject;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 size, Color color) {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            Vector2 anchor,
            Vector2 size,
            TextAnchor alignment,
            Color color
        ) {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha) {
            if (graphic == null) {
                return;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static Image FindImageByName(Transform root, string objectName) {
            foreach (Image image in root.GetComponentsInChildren<Image>(true)) {
                if (image.gameObject.name == objectName) {
                    return image;
                }
            }

            return null;
        }

        private static Text FindTextByName(Transform root, string objectName) {
            foreach (Text text in root.GetComponentsInChildren<Text>(true)) {
                if (text.gameObject.name == objectName) {
                    return text;
                }
            }

            return null;
        }

        private static void ConfigureText(
            Text text,
            Font font,
            string content,
            int? fontSize,
            FontStyle? fontStyle,
            TextAnchor alignment,
            Color color
        ) {
            if (text == null) {
                return;
            }

            text.font = font;
            if (content != null) {
                text.text = content;
            }

            if (fontSize.HasValue) {
                text.fontSize = fontSize.Value;
            }

            if (fontStyle.HasValue) {
                text.fontStyle = fontStyle.Value;
            }

            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void AddOutline(Graphic graphic) {
            if (graphic == null || graphic.GetComponent<Outline>() != null) {
                return;
            }

            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = outlineDistance;
            outline.useGraphicAlpha = true;
        }
    }

}
