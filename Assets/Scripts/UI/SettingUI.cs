using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Spelunky {

    public class SettingUI : MonoBehaviour {

        private static readonly Color DefaultActionOutlineColor = new Color(0.86f, 0.27f, 0.12f, 1f);

        private const string MasterRowName = "Master";
        private const string BackgroundRowName = "BGM";
        private const string SfxRowName = "SFX";
        private const string SliderTrackName = "SliderTrack";
        private const string FillBarName = "FillBar";
        private const string LabelTextName = "Text";
        private const string DragPointName = "DragPoint";
        private const string GotoTitleName = "GotoTitle";
        private const string GotoQuitName = "GotoQuit";

        public event Action VolumesChanged;

        public bool IsVisible => gameObject.activeSelf;

        [SerializeField] private string titleSceneName = "Title";
        [SerializeField] private Color actionTextOutlineColor = new Color(0.86f, 0.27f, 0.12f, 1f);
        [SerializeField] private Vector2 actionTextOutlineDistance = new Vector2(1.5f, -1.5f);
        [SerializeField] [Range(0.01f, 0.25f)] private float keyboardVolumeStep = 0.05f;

        private VolumeRow _masterRow;
        private VolumeRow _backgroundRow;
        private VolumeRow _sfxRow;
        private Button _gotoTitleButton;
        private Button _gotoQuitButton;
        private Text _gotoTitleText;
        private Text _gotoQuitText;
        private Outline _gotoTitleOutline;
        private Outline _gotoQuitOutline;
        private SettingEntry[] _entries = Array.Empty<SettingEntry>();
        private int _selectedEntryIndex;
        private bool _isApplyingSavedValues;

        private void Awake() {
            NormalizeActionOutlineColor();
            EnsureBindings();
            RefreshFromSavedVolumes();
        }

        private void OnEnable() {
            RefreshFromSavedVolumes();
        }

        private void Update() {
            if (!IsVisible) {
                return;
            }

            HandleActionNavigation();
            RefreshActionHighlightState();
        }

        public void SetVisible(bool isVisible) {
            if (isVisible) {
                EnsureBindings();
                RefreshFromSavedVolumes();
                transform.SetAsLastSibling();
            }
            else {
                ClearSelectionIfOwned();
            }

            gameObject.SetActive(isVisible);

            if (isVisible) {
                SelectPrimaryAction();
            }
        }

        public void RefreshFromSavedVolumes() {
            EnsureBindings();
            _isApplyingSavedValues = true;

            _masterRow.SetValue(AudioManager.GetSavedMasterVolume());
            _backgroundRow.SetValue(AudioManager.GetSavedBackgroundVolume());
            _sfxRow.SetValue(AudioManager.GetSavedSfxVolume());

            _isApplyingSavedValues = false;
        }

        private void EnsureBindings() {
            _masterRow = CreateOrUpdateRow(_masterRow, MasterRowName, AudioManager.GetSavedMasterVolume(), AudioManager.SetSavedMasterVolume);
            _backgroundRow = CreateOrUpdateRow(_backgroundRow, BackgroundRowName, AudioManager.GetSavedBackgroundVolume(), AudioManager.SetSavedBackgroundVolume);
            _sfxRow = CreateOrUpdateRow(_sfxRow, SfxRowName, AudioManager.GetSavedSfxVolume(), AudioManager.SetSavedSfxVolume);
            _gotoTitleButton = BindActionTextButton(_gotoTitleButton, ref _gotoTitleText, ref _gotoTitleOutline, GotoTitleName, HandleGotoTitleSelected);
            _gotoQuitButton = BindActionTextButton(_gotoQuitButton, ref _gotoQuitText, ref _gotoQuitOutline, GotoQuitName, HandleQuitSelected);
            RebuildEntries();
            RefreshActionHighlightState();
        }

        private VolumeRow CreateOrUpdateRow(VolumeRow row, string rowName, float initialValue, Action<float> applyVolume) {
            if (row.IsValid) {
                return row;
            }

            Transform rowTransform = transform.Find(rowName);
            if (rowTransform == null) {
                Debug.LogWarning($"SettingUI: Missing row '{rowName}'.", this);
                return row;
            }

            Transform trackTransform = rowTransform.Find(SliderTrackName);
            Transform fillTransform = trackTransform != null ? trackTransform.Find(FillBarName) : null;
            Transform labelTransform = rowTransform.Find(LabelTextName);
            RectTransform dragPointRect = FindDragPointForRow(rowTransform);
            Text rowLabelText = labelTransform != null ? labelTransform.GetComponent<Text>() : null;

            Image trackImage = trackTransform != null ? trackTransform.GetComponent<Image>() : null;
            Image fillImage = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
            RectTransform fillRect = fillTransform as RectTransform;

            if (trackImage == null || fillImage == null || fillRect == null || dragPointRect == null || rowLabelText == null) {
                Debug.LogWarning($"SettingUI: Missing slider visuals in row '{rowName}'.", this);
                return row;
            }

            FilledBarSliderInput input = dragPointRect.GetComponent<FilledBarSliderInput>();
            if (input == null) {
                input = dragPointRect.gameObject.AddComponent<FilledBarSliderInput>();
            }

            Image dragPointImage = dragPointRect.GetComponent<Image>();
            if (dragPointImage == null) {
                dragPointImage = dragPointRect.gameObject.AddComponent<Image>();
            }

            dragPointImage.color = new Color(1f, 1f, 1f, 0f);
            dragPointImage.raycastTarget = true;

            Outline labelOutline = EnsureOutline(rowLabelText);
            input.Initialize(dragPointRect, rowTransform.gameObject);

            ConfigureFillVisual(fillImage);
            fillImage.raycastTarget = false;
            trackImage.raycastTarget = false;

            VolumeRow createdRow = new VolumeRow(trackImage, fillImage, fillRect, input, applyVolume, rowTransform.gameObject, labelOutline);
            createdRow.SetValue(initialValue);
            createdRow.Bind(value => OnRowValueChanged(createdRow, value));
            return createdRow;
        }

        private RectTransform FindDragPointForRow(Transform rowTransform) {
            Transform childDragPoint = rowTransform.Find(DragPointName);
            if (childDragPoint is RectTransform childRect) {
                return childRect;
            }

            RectTransform rowRect = rowTransform as RectTransform;
            if (rowRect == null || transform.childCount == 0) {
                return null;
            }

            RectTransform bestMatch = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < transform.childCount; i++) {
                Transform candidate = transform.GetChild(i);
                if (candidate == null || candidate.name != DragPointName) {
                    continue;
                }

                RectTransform candidateRect = candidate as RectTransform;
                if (candidateRect == null) {
                    continue;
                }

                float distance = Mathf.Abs(candidateRect.anchoredPosition.y - rowRect.anchoredPosition.y);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    bestMatch = candidateRect;
                }
            }

            return bestMatch;
        }

        private void OnRowValueChanged(VolumeRow row, float value) {
            row.SetValue(value);

            if (_isApplyingSavedValues) {
                return;
            }

            row.ApplyValue(value);
            VolumesChanged?.Invoke();
        }

        private Button BindActionTextButton(Button button, ref Text targetText, ref Outline outline, string targetName, Action handler) {
            if (button != null) {
                return button;
            }

            Transform targetTransform = transform.Find(targetName);
            if (targetTransform == null) {
                return null;
            }

            targetText = targetTransform.GetComponent<Text>();
            if (targetText == null) {
                Debug.LogWarning($"SettingUI: Missing Text component on '{targetName}'.", this);
                return null;
            }

            targetText.raycastTarget = true;

            Button targetButton = targetTransform.GetComponent<Button>();
            if (targetButton == null) {
                targetButton = targetTransform.gameObject.AddComponent<Button>();
            }

            outline = ConfigureActionButtonVisuals(targetButton, targetText);
            targetButton.targetGraphic = targetText;
            targetButton.onClick.RemoveAllListeners();
            targetButton.onClick.AddListener(() => handler?.Invoke());
            return targetButton;
        }

        private Outline ConfigureActionButtonVisuals(Button button, Text targetText) {
            button.transition = Selectable.Transition.None;

            return EnsureOutline(targetText);
        }

        private void NormalizeActionOutlineColor() {
            float brightness = actionTextOutlineColor.r + actionTextOutlineColor.g + actionTextOutlineColor.b;
            if (brightness >= 2.4f) {
                actionTextOutlineColor = DefaultActionOutlineColor;
            }
        }

        private Outline EnsureOutline(Text targetText) {
            Outline outline = targetText.GetComponent<Outline>();
            if (outline == null) {
                outline = targetText.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = actionTextOutlineColor;
            outline.effectDistance = actionTextOutlineDistance;
            outline.enabled = false;
            return outline;
        }

        private static void ConfigureFillVisual(Image fillImage) {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillClockwise = true;
            fillImage.preserveAspect = false;
        }

        private void SelectPrimaryAction() {
            if (_entries == null || _entries.Length == 0) {
                return;
            }

            _selectedEntryIndex = 0;
            SelectEntry(_selectedEntryIndex);
        }

        private void ClearSelectionIfOwned() {
            if (EventSystem.current == null) {
                return;
            }

            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
            for (int i = 0; i < _entries.Length; i++) {
                if (currentSelection != _entries[i].SelectionObject) {
                    continue;
                }

                EventSystem.current.SetSelectedGameObject(null);
                break;
            }
        }

        private void HandleActionNavigation() {
            if (_entries == null || _entries.Length == 0) {
                return;
            }

            bool movedUp = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            bool movedDown = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            bool movedByTab = Input.GetKeyDown(KeyCode.Tab);
            bool movedLeft = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            bool movedRight = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

            if (currentSelection == null) {
                if (movedUp || movedDown || movedByTab || movedLeft || movedRight) {
                    SelectPrimaryAction();
                }

                return;
            }

            if (!SyncSelectedEntryIndex(currentSelection)) {
                if (movedUp || movedDown || movedByTab || movedLeft || movedRight) {
                    SelectPrimaryAction();
                }

                return;
            }

            if (movedUp) {
                MoveSelection(-1);
                return;
            }

            if (movedDown || movedByTab) {
                MoveSelection(1);
                return;
            }

            if (movedLeft) {
                AdjustSelectedVolume(-keyboardVolumeStep);
                return;
            }

            if (movedRight) {
                AdjustSelectedVolume(keyboardVolumeStep);
                return;
            }

            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.Space)) {
                return;
            }

            SettingEntry selectedEntry = _entries[_selectedEntryIndex];
            if (selectedEntry.Button != null) {
                selectedEntry.Button.onClick.Invoke();
            }
        }

        private void RefreshActionHighlightState() {
            GameObject currentSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int i = 0; i < _entries.Length; i++) {
                if (_entries[i].Outline == null) {
                    continue;
                }

                _entries[i].Outline.enabled = currentSelection == _entries[i].SelectionObject;
            }
        }

        private void RebuildEntries() {
            _entries = new[] {
                CreateVolumeEntry(_masterRow),
                CreateVolumeEntry(_backgroundRow),
                CreateVolumeEntry(_sfxRow),
                CreateActionEntry(_gotoTitleButton, _gotoTitleText, _gotoTitleOutline),
                CreateActionEntry(_gotoQuitButton, _gotoQuitText, _gotoQuitOutline)
            };
        }

        private SettingEntry CreateVolumeEntry(VolumeRow row) {
            return new SettingEntry {
                EntryType = SettingEntryType.Volume,
                SelectionObject = row.SelectionObject,
                Outline = row.LabelOutline,
                VolumeRow = row
            };
        }

        private static SettingEntry CreateActionEntry(Button button, Text text, Outline outline) {
            return new SettingEntry {
                EntryType = SettingEntryType.Action,
                SelectionObject = button != null ? button.gameObject : null,
                Outline = outline,
                Button = button
            };
        }

        private bool SyncSelectedEntryIndex(GameObject currentSelection) {
            for (int i = 0; i < _entries.Length; i++) {
                if (_entries[i].SelectionObject != currentSelection) {
                    continue;
                }

                _selectedEntryIndex = i;
                return true;
            }

            return false;
        }

        private void MoveSelection(int direction) {
            if (_entries == null || _entries.Length == 0) {
                return;
            }

            int nextIndex = _selectedEntryIndex + direction;
            if (nextIndex < 0) {
                nextIndex = _entries.Length - 1;
            }
            else if (nextIndex >= _entries.Length) {
                nextIndex = 0;
            }

            _selectedEntryIndex = nextIndex;
            SelectEntry(_selectedEntryIndex);
        }

        private void SelectEntry(int index) {
            if (EventSystem.current == null || index < 0 || index >= _entries.Length) {
                return;
            }

            GameObject selectionObject = _entries[index].SelectionObject;
            if (selectionObject == null) {
                return;
            }

            EventSystem.current.SetSelectedGameObject(selectionObject);
        }

        private void AdjustSelectedVolume(float delta) {
            if (_selectedEntryIndex < 0 || _selectedEntryIndex >= _entries.Length) {
                return;
            }

            SettingEntry entry = _entries[_selectedEntryIndex];
            if (entry.EntryType != SettingEntryType.Volume || !entry.VolumeRow.IsValid) {
                return;
            }

            entry.VolumeRow.SetValue(entry.VolumeRow.Value + delta);
            if (_isApplyingSavedValues) {
                return;
            }

            entry.VolumeRow.ApplyValue(entry.VolumeRow.Value);
            VolumesChanged?.Invoke();
        }

        private void HandleGotoTitleSelected() {
            if (SceneManager.GetActiveScene().name == titleSceneName) {
                SetVisible(false);
                return;
            }

            if (UIManager.Instance != null) {
                UIManager.Instance.ResetGameplayUI();
            }
            else {
                SetVisible(false);
            }

            SceneManager.LoadScene(titleSceneName);
        }

        private void HandleQuitSelected() {
            SetVisible(false);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        [Serializable]
        private struct VolumeRow {

            private readonly Image _trackImage;
            private readonly Image _fillImage;
            private readonly RectTransform _fillRect;
            private readonly FilledBarSliderInput _input;
            private readonly Action<float> _applyValue;
            private readonly GameObject _selectionObject;
            private readonly Outline _labelOutline;

            public bool IsValid => _trackImage != null && _fillImage != null && _fillRect != null && _input != null && _applyValue != null && _selectionObject != null;
            public GameObject SelectionObject => _selectionObject;
            public Outline LabelOutline => _labelOutline;
            public float Value => _input != null ? _input.Value : 0f;

            public VolumeRow(Image trackImage, Image fillImage, RectTransform fillRect, FilledBarSliderInput input, Action<float> applyValue, GameObject selectionObject, Outline labelOutline) {
                _trackImage = trackImage;
                _fillImage = fillImage;
                _fillRect = fillRect;
                _input = input;
                _applyValue = applyValue;
                _selectionObject = selectionObject;
                _labelOutline = labelOutline;
            }

            public void Bind(Action<float> onValueChanged) {
                if (!IsValid) {
                    return;
                }

                _input.onValueChanged -= onValueChanged;
                _input.onValueChanged += onValueChanged;
            }

            public void SetValue(float value) {
                if (!IsValid) {
                    return;
                }

                float clampedValue = Mathf.Clamp01(value);
                _fillImage.fillAmount = clampedValue;
                _input.SetValueWithoutNotify(clampedValue);
            }

            public void ApplyValue(float value) {
                if (!IsValid) {
                    return;
                }

                _applyValue.Invoke(Mathf.Clamp01(value));
            }

        }

        private enum SettingEntryType {
            Volume,
            Action
        }

        private struct SettingEntry {
            public SettingEntryType EntryType;
            public GameObject SelectionObject;
            public Outline Outline;
            public VolumeRow VolumeRow;
            public Button Button;
        }

    }

    public class FilledBarSliderInput : MonoBehaviour, IPointerDownHandler, IDragHandler {

        public event Action<float> onValueChanged;

        private RectTransform _trackRect;
        private GameObject _selectTarget;
        private float _value;

        public float Value => _value;

        public void Initialize(RectTransform trackRect, GameObject selectTarget) {
            _trackRect = trackRect;
            _selectTarget = selectTarget;
        }

        public void SetValueWithoutNotify(float value) {
            _value = Mathf.Clamp01(value);
        }

        public void OnPointerDown(PointerEventData eventData) {
            SelectOwner();
            UpdateFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData) {
            SelectOwner();
            UpdateFromPointer(eventData);
        }

        private void SelectOwner() {
            if (EventSystem.current == null || _selectTarget == null) {
                return;
            }

            EventSystem.current.SetSelectedGameObject(_selectTarget);
        }

        private void UpdateFromPointer(PointerEventData eventData) {
            if (_trackRect == null) {
                _trackRect = transform as RectTransform;
            }

            if (_trackRect == null) {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_trackRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) {
                return;
            }

            Rect rect = _trackRect.rect;
            if (rect.width <= 0f) {
                return;
            }

            float normalized = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            _value = Mathf.Clamp01(normalized);
            onValueChanged?.Invoke(_value);
        }

    }

}
