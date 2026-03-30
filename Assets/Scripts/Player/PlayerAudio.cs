using UnityEngine;

namespace Spelunky {

    public class PlayerAudio : MonoBehaviour {

        public AudioClip jumpClip;
        public AudioClip landClip;
        public AudioClip grabClip;
        public AudioClip whipClip;

        private AudioSource _audioSource;

        private void Awake() {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource != null && AudioManager.Instance != null) {
                AudioManager.Instance.ConfigureSource(_audioSource, AudioManager.AudioGroup.SFX);
            }
        }

        public void Play(AudioClip clip, float volume = 1f) {
            _audioSource.PlayOneShot(clip, volume);
        }

    }

}
