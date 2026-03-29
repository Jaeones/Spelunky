using UnityEngine;

namespace Spelunky {

    public class ArrowTrap : MonoBehaviour, ITickable {

        [SerializeField] private ThrowableItem arrowPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float arrowSpeed = 200f;
        [SerializeField] private int fireDirection = -1;
        [SerializeField] private float rayDistance = 160f;
        [SerializeField] private LayerMask detectMask;
        [SerializeField] private LayerMask occlusionMask;
        [SerializeField] private AudioClip fireSound;

        private bool _hasFired;

        private Vector2 RayOrigin => firePoint != null ? firePoint.position : transform.position;
        private Vector2 RayDirection => new Vector2(fireDirection, 0);

        private void OnEnable() {
            EntityManager.Instance?.Register(this);
        }

        private void OnDisable() {
            EntityManager.Instance?.Unregister(this);
        }

        // ITickable implementation
        public bool IsTickActive => !_hasFired;

        public void Tick() {
            int activeOcclusionMask = occlusionMask.value != 0 ? occlusionMask.value : GetDefaultOcclusionMask();
            int combinedMask = detectMask.value | activeOcclusionMask;
            RaycastHit2D hit = Physics2D.Raycast(RayOrigin, RayDirection, rayDistance, combinedMask);
            if (hit.collider != null && ShouldTriggerFromHit(hit.collider)) {
                Fire();
            }
        }

        private static int GetDefaultOcclusionMask() {
            return LayerMask.GetMask("Obstacle", "Block", "Indestructable");
        }

        private bool ShouldTriggerFromHit(Collider2D hitCollider) {
            if (hitCollider == null || !IsLayerInMask(hitCollider.gameObject.layer, detectMask)) {
                return false;
            }

            int ladderLayer = LayerMask.NameToLayer("Ladder");
            if (ladderLayer >= 0 && hitCollider.gameObject.layer == ladderLayer) {
                return hitCollider.CompareTag("Rope") || hitCollider.GetComponentInParent<Rope>() != null;
            }

            return true;
        }

        private static bool IsLayerInMask(int layer, LayerMask mask) {
            return (mask.value & (1 << layer)) != 0;
        }

        private void Fire() {
            _hasFired = true;

            ThrowableItem arrow = Instantiate(arrowPrefab, RayOrigin, Quaternion.identity);

            Vector2 velocity = new Vector2(arrowSpeed * fireDirection, 0);
            arrow.OnThrown(null, velocity, true);

            if (fireSound != null && AudioManager.Instance != null) {
                AudioManager.Instance.PlaySoundAtPosition(fireSound, transform.position, AudioManager.AudioGroup.SFX);
            }
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 end = origin + new Vector3(fireDirection * rayDistance, 0, 0);
            Gizmos.DrawLine(origin, end);
        }

    }

}
