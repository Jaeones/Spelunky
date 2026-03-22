using UnityEngine;

namespace Spelunky {

    public enum InventoryItemType {
        Bomb,
        Rope
    }

    public class InventoryPickup : MonoBehaviour {

        public InventoryItemType itemType;
        public int amount = 4;

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

                switch (itemType) {
                    case InventoryItemType.Bomb:
                        player.Inventory.PickupBombs(amount);
                        break;
                    case InventoryItemType.Rope:
                        player.Inventory.PickupRopes(amount);
                        break;
                }

                Destroy(gameObject);
            }
        }

    }

}
