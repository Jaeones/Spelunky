using UnityEngine;

namespace Spelunky {

    public class Exit : MonoBehaviour {

        public GameObject buttonPromptObject;
        private bool _hasHandledEntry;

        private void Awake() {
            buttonPromptObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (_hasHandledEntry) {
                return;
            }

            Player player = other.GetComponent<Player>();
            if (player != null) {
                buttonPromptObject.SetActive(true);
                player.EnteredDoorway(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (_hasHandledEntry) {
                return;
            }

            Player player = other.GetComponent<Player>();
            if (player != null) {
                buttonPromptObject.SetActive(false);
                player.ExitedDoorway(this);
            }
        }

        public void HandlePlayerEntered(Player player) {
            if (_hasHandledEntry) {
                return;
            }

            _hasHandledEntry = true;
            buttonPromptObject.SetActive(false);

            Collider2D exitCollider = GetComponent<Collider2D>();
            if (exitCollider != null) {
                exitCollider.enabled = false;
            }

            GameManager.Instance?.HandlePlayerEnteredExit(player, this);
        }

        public bool CanPlayerEnter(Player player) {
            if (_hasHandledEntry) {
                return false;
            }

            return GameManager.Instance == null || GameManager.Instance.CanPlayerEnterExit(player, this);
        }

        public void HandleLockedAttempt(Player player) {
            GameManager.Instance?.HandleLockedExitAttempt(player, this);
        }

    }

}
