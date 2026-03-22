using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spelunky {

    public enum AccessoryType {
        ClimbingGlove,
        SpringBoots,
        PitchersMitt,
        Paste
    }

    [RequireComponent(typeof(Player))]
    public class PlayerAccessories : MonoBehaviour {

        public event Action<AccessoryType, Sprite> AccessoryAdded;

        private HashSet<AccessoryType> _accessories = new HashSet<AccessoryType>();

        public bool HasClimbingGlove => _accessories.Contains(AccessoryType.ClimbingGlove);
        public bool HasSpringBoots => _accessories.Contains(AccessoryType.SpringBoots);
        public bool HasPitchersMitt => _accessories.Contains(AccessoryType.PitchersMitt);
        public bool HasPaste => _accessories.Contains(AccessoryType.Paste);

        public float JumpHeightBonus => HasSpringBoots ? 16f : 0f;

        public void AddAccessory(AccessoryType type, Sprite icon = null, bool notifyUi = true) {
            Debug.Log($"Trying to add {type} accessory.");
            if (_accessories.Add(type)) {
                Debug.Log($"Added {type} accessory.");
                if (notifyUi) {
                    AccessoryAdded?.Invoke(type, icon);
                }
            }
        }

        public bool HasAccessory(AccessoryType type) {
            return _accessories.Contains(type);
        }

        public void RemoveAccessory(AccessoryType type) {
            _accessories.Remove(type);
        }

        public List<string> GetAccessoryIds() {
            List<string> accessoryIds = new List<string>();
            foreach (AccessoryType accessory in _accessories) {
                accessoryIds.Add(accessory.ToString());
            }

            return accessoryIds;
        }

        public void SetAccessories(IEnumerable<string> accessoryIds, bool notifyUi = true) {
            _accessories.Clear();

            if (accessoryIds == null) {
                return;
            }

            foreach (string accessoryId in accessoryIds) {
                if (!Enum.TryParse(accessoryId, out AccessoryType accessoryType)) {
                    continue;
                }

                AddAccessory(accessoryType, null, notifyUi);
            }
        }

    }

}
