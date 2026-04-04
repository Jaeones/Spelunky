using UnityEngine;

namespace Spelunky {

    /// <summary>
    /// TODO: Replace with new input system. To be honest I thought I had done that ages ago.
    /// </summary>
    [RequireComponent(typeof(Player))]
    public class PlayerInput : MonoBehaviour, IEarlyTickable {

        public float joystickDeadzone;

        private Player _player;

        private void Awake() {
            _player = GetComponent<Player>();
        }

        private void OnEnable() {
            if (_player == null) {
                _player = GetComponent<Player>();
            }

            EntityManager.Instance?.RegisterEarlyTickable(this);
        }

        private void OnDisable() {
            EntityManager.Instance?.UnregisterEarlyTickable(this);
        }

        // IEarlyTickable implementation
        public void EarlyTick() {
            if (_player == null) {
                _player = GetComponent<Player>();
                if (_player == null) {
                    return;
                }
            }

            PlayerState currentState = _player.CurrentPlayerState;
            if (currentState == null) {
                return;
            }

            if (currentState.LockInput()) {
                return;
            }

            if (UIManager.Instance != null && UIManager.Instance.IsSettingsOpen) {
                currentState.OnDirectionalInput(Vector2.zero);
                _player.sprinting = false;
                return;
            }

            Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            directionalInput.x = Mathf.Abs(directionalInput.x) < joystickDeadzone ? 0 : directionalInput.x;
            directionalInput.y = Mathf.Abs(directionalInput.y) < joystickDeadzone ? 0 : directionalInput.y;
            currentState.OnDirectionalInput(directionalInput);

            _player.sprinting = Input.GetButton("Sprint Keyboard") || Input.GetAxisRaw("Sprint Controller") != 0;

            if (Input.GetButtonDown("Jump")) {
                currentState.OnJumpInputDown();
            }

            if (Input.GetButtonUp("Jump")) {
                currentState.OnJumpInputUp();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Joystick1Button1)) {
                currentState.OnBombInputDown();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Joystick1Button3)) {
                currentState.OnRopeInputDown();
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Joystick1Button5)) {
                currentState.OnUseInputDown();
            }

            bool attackPressed = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Joystick1Button2);
            bool blockAttackInput = GameManager.Instance != null && GameManager.Instance.IsAttackInputTemporarilyBlocked;
            if (attackPressed && !blockAttackInput) {
                currentState.OnAttackInputDown();
            }
        }

    }

}
