using UnityEngine;
using UnityEngine.UI;

namespace Spelunky {

    /// <summary>
    /// Scene-side HUD binding for PlayerUI.
    /// Inspector references are preferred, but missing fields can still be auto-filled
    /// from children under the bound HUD root during the migration period.
    /// </summary>
    public class PlayerHUDReferences : MonoBehaviour {

        [Header("HUD Text")]
        [SerializeField] private Text lifeAmountText;
        [SerializeField] private Text bombAmountText;
        [SerializeField] private Text ropeAmountText;
        [SerializeField] private Text totalGoldAmountText;
        [SerializeField] private Text currentGoldAmountText;

        [Header("HUD Objects")]
        [SerializeField] private Transform accessoriesContainer;
        [SerializeField] private GameObject canvasRoot;

        public Text LifeAmountText => lifeAmountText;
        public Text BombAmountText => bombAmountText;
        public Text RopeAmountText => ropeAmountText;
        public Text TotalGoldAmountText => totalGoldAmountText;
        public Text CurrentGoldAmountText => currentGoldAmountText;
        public Transform AccessoriesContainer => accessoriesContainer;
        public GameObject CanvasRoot => canvasRoot != null ? canvasRoot : gameObject;

        public bool HasRequiredReferences =>
            lifeAmountText != null &&
            bombAmountText != null &&
            ropeAmountText != null &&
            totalGoldAmountText != null &&
            currentGoldAmountText != null &&
            accessoriesContainer != null;

        private void Awake() {
            AutoAssignMissingReferences();
        }

        private void Reset() {
            AutoAssignMissingReferences();
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Application.isPlaying) {
                AutoAssignMissingReferences();
            }
        }
#endif

        public void AutoAssignMissingReferences() {
            if (canvasRoot == null) {
                canvasRoot = gameObject;
            }

            if (lifeAmountText == null) {
                lifeAmountText = FindTextWithinRoot("LifeAmountText");
            }

            if (bombAmountText == null) {
                bombAmountText = FindTextWithinRoot("BombAmountText");
            }

            if (ropeAmountText == null) {
                ropeAmountText = FindTextWithinRoot("RopeAmountText");
            }

            if (totalGoldAmountText == null) {
                totalGoldAmountText = FindTextWithinRoot("TotalGoldAmountText");
            }

            if (currentGoldAmountText == null) {
                currentGoldAmountText = FindTextWithinRoot("CurrentGoldAmountText");
            }

            if (accessoriesContainer == null) {
                accessoriesContainer = FindTransformWithinRoot("AccessoriesUIContainer");
            }
        }

        private Text FindTextWithinRoot(string objectName) {
            Transform target = FindTransformWithinRoot(objectName);
            return target != null ? target.GetComponent<Text>() : null;
        }

        private Transform FindTransformWithinRoot(string objectName) {
            return FindChildRecursive(transform, objectName);
        }

        private static Transform FindChildRecursive(Transform parent, string targetName) {
            if (parent == null) {
                return null;
            }

            if (parent.name == targetName) {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++) {
                Transform child = parent.GetChild(i);
                Transform result = FindChildRecursive(child, targetName);
                if (result != null) {
                    return result;
                }
            }

            return null;
        }

    }

}
