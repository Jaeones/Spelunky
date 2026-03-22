using System.Collections;
using TwiiK.Utility;
using UnityEngine;

namespace Spelunky {

    /// <summary>
    /// The state we're in when we're entering a door (exiting a level).
    /// </summary>
    public class PlayerEnterDoorState : PlayerState {

        public SpriteAnimation enterDoorAnimation;
        public AudioClip enterDoorClip;

        public override bool CanEnterState() {
            if (player._exitDoor == null) {
                return false;
            }

            return true;
        }

        public override void EnterState() {
            StartCoroutine(EnterDoor());
        }

        public override void ChangePlayerVelocity(ref Vector2 velocity) {
            velocity = Vector2.zero;
        }

        public override bool LockInput() {
            return true;
        }

        private IEnumerator EnterDoor() {
            Exit exitDoor = player._exitDoor;
            if (exitDoor == null) {
                yield break;
            }

            player.Physics.SetPosition(new Vector2(exitDoor.transform.position.x + Tile.Width / 2f, exitDoor.transform.position.y));

            player.Visuals.animator.Play(enterDoorAnimation);

            player.Audio.Play(enterDoorClip);

            Color color = player.Visuals.renderer.color;
            float animationLength = player.Visuals.animator.GetAnimationLength(enterDoorAnimation);
            float t = 0;
            while (t <= animationLength) {
                t += Time.deltaTime;
                player.Visuals.renderer.color = Color.Lerp(color, Color.black, t.Remap(0f, animationLength, 0f, 1f));
                yield return null;
            }

            exitDoor?.HandlePlayerEntered(player);
        }

    }

}
