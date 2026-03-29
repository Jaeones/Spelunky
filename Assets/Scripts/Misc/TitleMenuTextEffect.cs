using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spelunky {

    public class TitleMenuTextEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {

        [SerializeField] private Color highlightColor = new Color(1f, 0.91f, 0.54f, 1f);
        [SerializeField] private Vector3 highlightedScale = new Vector3(1.08f, 1.08f, 1f);
        [SerializeField] private Vector2 highlightedOutlineDistance = new Vector2(2f, -2f);

        private Text _text;
        private Outline _outline;
        private Color _defaultColor;
        private Vector3 _defaultScale;
        private bool _isHovered;
        private bool _isSelected;

        private void Awake() {
            _text = GetComponent<Text>();
            _outline = GetComponent<Outline>();

            if (_outline == null) {
                _outline = gameObject.AddComponent<Outline>();
            }

            _defaultColor = _text != null ? _text.color : Color.white;
            _defaultScale = transform.localScale;
            _outline.effectColor = new Color(0.2f, 0.12f, 0.04f, 0.9f);
            _outline.effectDistance = Vector2.zero;
            _outline.useGraphicAlpha = true;
        }

        public void OnPointerEnter(PointerEventData eventData) {
            _isHovered = true;
            UpdateVisualState();
        }

        public void OnPointerExit(PointerEventData eventData) {
            _isHovered = false;
            UpdateVisualState();
        }

        public void OnSelect(BaseEventData eventData) {
            _isSelected = true;
            UpdateVisualState();
        }

        public void OnDeselect(BaseEventData eventData) {
            _isSelected = false;
            UpdateVisualState();
        }

        private void UpdateVisualState() {
            bool highlighted = _isHovered || _isSelected;
            if (_text != null) {
                _text.color = highlighted ? highlightColor : _defaultColor;
            }

            transform.localScale = highlighted ? highlightedScale : _defaultScale;

            if (_outline != null) {
                _outline.effectDistance = highlighted ? highlightedOutlineDistance : Vector2.zero;
            }
        }

    }

}
