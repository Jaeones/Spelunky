using UnityEngine;

namespace Spelunky {

    public class AccessoryPickup : MonoBehaviour {

        public AccessoryType accessoryType;

        [Tooltip("Icon to show in the UI. If not set, uses the SpriteRenderer's sprite.")]
        public Sprite icon;

        private bool _isCollected;

        private void OnTriggerEnter2D(Collider2D other) {
            if (_isCollected) {
                return;
            }

            var player = other.GetComponentInParent<Player>();
            if (player != null) {
                _isCollected = true;
                Collider2D pickupCollider = GetComponent<Collider2D>();
                if (pickupCollider != null) {
                    pickupCollider.enabled = false;
                }

                Sprite uiIcon = icon;
                if (uiIcon == null) {
                    var spriteRenderer = GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null) {
                        uiIcon = spriteRenderer.sprite;
                    }
                }

                player.Accessories.AddAccessory(accessoryType, uiIcon);
                Destroy(gameObject);
            }
        }

    }

}
