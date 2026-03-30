using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spelunky {

    public class SettingUI : MonoBehaviour {

        private const string MasterRowName = "Master";
        private const string BackgroundRowName = "BGM";
        private const string SfxRowName = "SFX";
        private const string SliderTrackName = "SliderTrack";
        private const string FillBarName = "FillBar";
        private const string DragPointName = "DragPoint";

        public event Action VolumesChanged;

        public bool IsVisible => gameObject.activeSelf;

        private VolumeRow _masterRow;
        private VolumeRow _backgroundRow;
        private VolumeRow _sfxRow;
        private bool _isApplyingSavedValues;

        private void Awake() {
            EnsureBindings();
            RefreshFromSavedVolumes();
        }

        private void OnEnable() {
            RefreshFromSavedVolumes();
        }

        public void SetVisible(bool isVisible) {
            if (isVisible) {
                RefreshFromSavedVolumes();
                transform.SetAsLastSibling();
            }

            gameObject.SetActive(isVisible);
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
            RectTransform dragPointRect = FindDragPointForRow(rowTransform);

            Image trackImage = trackTransform != null ? trackTransform.GetComponent<Image>() : null;
            Image fillImage = fillTransform != null ? fillTransform.GetComponent<Image>() : null;

            if (trackImage == null || fillImage == null || dragPointRect == null) {
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

            input.Initialize(dragPointRect);

            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillClockwise = true;
            fillImage.raycastTarget = false;
            trackImage.raycastTarget = false;

            VolumeRow createdRow = new VolumeRow(trackImage, fillImage, input, applyVolume);
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

        [Serializable]
        private struct VolumeRow {

            private readonly Image _trackImage;
            private readonly Image _fillImage;
            private readonly FilledBarSliderInput _input;
            private readonly Action<float> _applyValue;

            public bool IsValid => _trackImage != null && _fillImage != null && _input != null && _applyValue != null;

            public VolumeRow(Image trackImage, Image fillImage, FilledBarSliderInput input, Action<float> applyValue) {
                _trackImage = trackImage;
                _fillImage = fillImage;
                _input = input;
                _applyValue = applyValue;
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

    }

    public class FilledBarSliderInput : MonoBehaviour, IPointerDownHandler, IDragHandler {

        public event Action<float> onValueChanged;

        private RectTransform _trackRect;
        private float _value;

        public void Initialize(RectTransform trackRect) {
            _trackRect = trackRect;
        }

        public void SetValueWithoutNotify(float value) {
            _value = Mathf.Clamp01(value);
        }

        public void OnPointerDown(PointerEventData eventData) {
            UpdateFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData) {
            UpdateFromPointer(eventData);
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
