using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spelunky {

    [RequireComponent(typeof(Player))]
    public class PlayerUI : MonoBehaviour {

        [Header("HUD Binding")]
        [SerializeField] private PlayerHUDReferences hudBinding;

        [Header("Accessories")]
        public GameObject accessoryIconPrefab;
        public Transform accessoriesContainer;
        [SerializeField] private Vector2 accessoryIconDisplaySize = new Vector2(48f, 48f);

        private static readonly Dictionary<AccessoryType, Sprite> AccessoryIconCache = new Dictionary<AccessoryType, Sprite>();

        private Player _player;
        private Text _lifeAmountText;
        private Text _bombAmountText;
        private Text _ropeAmountText;
        private Text _totalGoldAmountText;
        private Text _currentGoldAmountText;

        private int _currentGoldAmount;
        private int _totalGoldAmount;

        public float timeBeforeAddingCurrentGoldToTotal;
        private float _goldAddTimer;
        public int goldToAddPerInterval;
        public float goldIntervalTime;
        private float _intervalTimer;

        private GameObject _canvasObject;

        private void Awake() {
            _player = GetComponent<Player>();
            if (!TryResolveHudReferences()) {
                Debug.LogError("PlayerUI: Failed to resolve HUD references. Assign a PlayerHUDReferences binding in the scene.", this);
                enabled = false;
                return;
            }

            _player.Health.HealthChangedEvent.AddListener(OnHealthChanged);
            _player.Inventory.BombsChangedEvent.AddListener(OnBombsChanged);
            _player.Inventory.RopesChangedEvent.AddListener(OnRopesChanged);
            _player.Inventory.GoldAmountChangedEvent.AddListener(OnGoldChanged);
            _player.Accessories.AccessoryAdded += OnAccessoryAdded;

            RefreshAllValues();
            RefreshCanvasHack();
        }

        private IEnumerator Start() {
            // RunManager restores persisted accessories after player instantiation.
            // Re-sync once on the next frame so stage transitions rebuild the HUD from real state.
            yield return null;
            SyncAccessoryHudToPlayerState();
        }

        private void Update() {
            if (_currentGoldAmount <= 0) {
                _goldAddTimer = 0;
                _currentGoldAmountText.gameObject.SetActive(false);
                return;
            }

            _currentGoldAmountText.gameObject.SetActive(true);

            _goldAddTimer += Time.deltaTime;
            if (_goldAddTimer < timeBeforeAddingCurrentGoldToTotal) {
                return;
            }

            _intervalTimer += Time.deltaTime;
            if (_intervalTimer < goldIntervalTime) {
                return;
            }

            UpdateUIGoldAmount();
            _intervalTimer = 0;
        }

        private void OnHealthChanged() {
            _lifeAmountText.text = _player.Health.CurrentHealth.ToString();
        }

        private void OnBombsChanged() {
            _bombAmountText.text = _player.Inventory.numberOfBombs.ToString();
        }

        private void OnRopesChanged() {
            _ropeAmountText.text = _player.Inventory.numberOfRopes.ToString();
        }

        private void OnGoldChanged(int amount) {
            _goldAddTimer = 0;
            _intervalTimer = 0;
            _currentGoldAmount += amount;
            _totalGoldAmount = _player.Inventory.goldAmount - _currentGoldAmount;
            _currentGoldAmountText.text = " +" + _currentGoldAmount;
            _totalGoldAmountText.text = _totalGoldAmount.ToString();
        }

        private void UpdateUIGoldAmount() {
            int goldToAdd = goldToAddPerInterval > _currentGoldAmount ? _currentGoldAmount : goldToAddPerInterval;
            _currentGoldAmount -= goldToAdd;
            _totalGoldAmount += goldToAdd;
            _currentGoldAmountText.text = " +" + _currentGoldAmount;
            _totalGoldAmountText.text = _totalGoldAmount.ToString();
        }

        private void OnAccessoryAdded(AccessoryType type, Sprite icon) {
            Sprite resolvedIcon = ResolveAccessoryIcon(type, icon);
            if (resolvedIcon == null) {
                return;
            }

            AddAccessoryIcon(resolvedIcon);
        }

        private void OnDestroy() {
            if (_player != null) {
                if (_player.Health != null) {
                    _player.Health.HealthChangedEvent.RemoveListener(OnHealthChanged);
                }

                if (_player.Inventory != null) {
                    _player.Inventory.BombsChangedEvent.RemoveListener(OnBombsChanged);
                    _player.Inventory.RopesChangedEvent.RemoveListener(OnRopesChanged);
                    _player.Inventory.GoldAmountChangedEvent.RemoveListener(OnGoldChanged);
                }
            }

            if (_player != null && _player.Accessories != null) {
                _player.Accessories.AccessoryAdded -= OnAccessoryAdded;
            }
        }

        private bool TryResolveHudReferences() {
            if (TryApplyBinding(hudBinding)) {
                return true;
            }

            UIManager manager = UIManager.EnsureInstance();
            if (manager != null && TryApplyBinding(manager.PlayerHUD)) {
                return true;
            }

            if (TryApplyBinding(FindObjectOfType<PlayerHUDReferences>())) {
                return true;
            }

            return TryResolveLegacyHudReferences();
        }

        private bool TryApplyBinding(PlayerHUDReferences binding) {
            if (binding == null) {
                return false;
            }

            binding.AutoAssignMissingReferences();

            _lifeAmountText = binding.LifeAmountText;
            _bombAmountText = binding.BombAmountText;
            _ropeAmountText = binding.RopeAmountText;
            _totalGoldAmountText = binding.TotalGoldAmountText;
            _currentGoldAmountText = binding.CurrentGoldAmountText;
            accessoriesContainer = binding.AccessoriesContainer;
            _canvasObject = binding.CanvasRoot;
            hudBinding = binding;

            return HasResolvedHudReferences();
        }

        private bool TryResolveLegacyHudReferences() {
            // Temporary fallback for scenes that have not been wired through PlayerHUDReferences or UIManager yet.
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            for (int i = 0; i < canvases.Length; i++) {
                if (!TryResolveLegacyHudReferencesFromRoot(canvases[i].transform)) {
                    continue;
                }

                _canvasObject = canvases[i].gameObject;
                return true;
            }

            return false;
        }

        private bool HasResolvedHudReferences() {
            return _lifeAmountText != null &&
                _bombAmountText != null &&
                _ropeAmountText != null &&
                _totalGoldAmountText != null &&
                _currentGoldAmountText != null &&
                accessoriesContainer != null &&
                _canvasObject != null;
        }

        private bool TryResolveLegacyHudReferencesFromRoot(Transform root) {
            if (root == null) {
                return false;
            }

            _lifeAmountText = FindTextWithinRoot(root, "LifeAmountText");
            _bombAmountText = FindTextWithinRoot(root, "BombAmountText");
            _ropeAmountText = FindTextWithinRoot(root, "RopeAmountText");
            _totalGoldAmountText = FindTextWithinRoot(root, "TotalGoldAmountText");
            _currentGoldAmountText = FindTextWithinRoot(root, "CurrentGoldAmountText");
            accessoriesContainer = FindChildRecursive(root, "AccessoriesUIContainer");

            return _lifeAmountText != null &&
                _bombAmountText != null &&
                _ropeAmountText != null &&
                _totalGoldAmountText != null &&
                _currentGoldAmountText != null &&
                accessoriesContainer != null;
        }

        private static Text FindTextWithinRoot(Transform root, string objectName) {
            Transform target = FindChildRecursive(root, objectName);
            return target != null ? target.GetComponent<Text>() : null;
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

        private void RefreshAllValues() {
            OnHealthChanged();
            OnBombsChanged();
            OnRopesChanged();

            _currentGoldAmount = 0;
            _totalGoldAmount = _player.Inventory.goldAmount;
            _currentGoldAmountText.text = " +0";
            _currentGoldAmountText.gameObject.SetActive(false);
            _totalGoldAmountText.text = _totalGoldAmount.ToString();
            SyncAccessoryHudToPlayerState();
        }

        private void RefreshCanvasHack() {
            if (_canvasObject == null) {
                return;
            }

            // Temporary compatibility hack until the HUD background is driven by layout instead of a forced refresh.
            _canvasObject.SetActive(false);
            _canvasObject.SetActive(true);
        }

        private void SyncAccessoryHudToPlayerState() {
            if (accessoriesContainer == null || _player == null || _player.Accessories == null) {
                return;
            }

            ClearAccessoryIcons();

            AccessoryType[] accessoryTypes = {
                AccessoryType.ClimbingGlove,
                AccessoryType.SpringBoots,
                AccessoryType.PitchersMitt,
                AccessoryType.Paste
            };

            for (int i = 0; i < accessoryTypes.Length; i++) {
                AccessoryType accessoryType = accessoryTypes[i];
                if (!_player.Accessories.HasAccessory(accessoryType)) {
                    continue;
                }

                Sprite icon = ResolveAccessoryIcon(accessoryType);
                if (icon == null) {
                    continue;
                }

                AddAccessoryIcon(icon);
            }
        }

        private void ClearAccessoryIcons() {
            for (int i = accessoriesContainer.childCount - 1; i >= 0; i--) {
                Transform child = accessoriesContainer.GetChild(i);
                if (child == null) {
                    continue;
                }

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private Sprite ResolveAccessoryIcon(AccessoryType type, Sprite icon = null) {
            if (icon != null) {
                AccessoryIconCache[type] = icon;
                return icon;
            }

            if (AccessoryIconCache.TryGetValue(type, out Sprite cachedIcon) && cachedIcon != null) {
                return cachedIcon;
            }

            Sprite resolvedIcon = FindAccessoryIconInScene(type);
            if (resolvedIcon == null) {
                resolvedIcon = LoadAccessoryIconFromPrefab(type);
            }

            if (resolvedIcon != null) {
                AccessoryIconCache[type] = resolvedIcon;
            }

            return resolvedIcon;
        }

        private static Sprite FindAccessoryIconInScene(AccessoryType type) {
            AccessoryPickup[] pickups = FindObjectsOfType<AccessoryPickup>(true);
            for (int i = 0; i < pickups.Length; i++) {
                AccessoryPickup pickup = pickups[i];
                if (pickup == null || pickup.accessoryType != type) {
                    continue;
                }

                if (pickup.icon != null) {
                    return pickup.icon;
                }

                SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null) {
                    return renderer.sprite;
                }
            }

            return null;
        }

        private static Sprite LoadAccessoryIconFromPrefab(AccessoryType type) {
#if UNITY_EDITOR
            string prefabPath;
            switch (type) {
                case AccessoryType.ClimbingGlove:
                    prefabPath = "Assets/Prefabs/Items/Accessories/ClimbingGlove.prefab";
                    break;
                case AccessoryType.SpringBoots:
                    prefabPath = "Assets/Prefabs/Items/Accessories/SpringBoots.prefab";
                    break;
                case AccessoryType.PitchersMitt:
                    prefabPath = "Assets/Prefabs/Items/Accessories/PitchersMitt.prefab";
                    break;
                case AccessoryType.Paste:
                    prefabPath = "Assets/Prefabs/Items/Accessories/Paste.prefab";
                    break;
                default:
                    return null;
            }

            GameObject accessoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (accessoryPrefab == null) {
                return null;
            }

            AccessoryPickup pickup = accessoryPrefab.GetComponent<AccessoryPickup>();
            if (pickup != null && pickup.icon != null) {
                return pickup.icon;
            }

            SpriteRenderer renderer = accessoryPrefab.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
#else
            return null;
#endif
        }

        private void AddAccessoryIcon(Sprite icon) {
            if (accessoriesContainer == null || accessoryIconPrefab == null || icon == null) {
                return;
            }

            GameObject iconInstance = Instantiate(accessoryIconPrefab, accessoriesContainer);
            RectTransform iconRect = iconInstance.transform as RectTransform;
            if (iconRect != null) {
                iconRect.localScale = Vector3.one;
                iconRect.sizeDelta = accessoryIconDisplaySize;
            }

            Image image = iconInstance.GetComponent<Image>();
            if (image != null) {
                image.sprite = icon;
                image.preserveAspect = true;
            }

            RectTransform containerRect = accessoriesContainer as RectTransform;
            if (containerRect != null && containerRect.sizeDelta.y < accessoryIconDisplaySize.y) {
                containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, accessoryIconDisplaySize.y);
            }
        }

    }

}
