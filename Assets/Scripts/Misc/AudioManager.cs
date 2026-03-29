using TwiiK.Utility;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager> {

    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ambientGroup;
    public AudioMixerGroup musicGroup;

    [Header("Stage Music")]
    [SerializeField] private AudioClip stage1Music;
    [SerializeField] private AudioClip stage2Music;
    [SerializeField] private AudioClip stage3Music;
    [SerializeField] private AudioClip stage4Music;

    public enum AudioGroup {

        SFX,
        Ambient,
        Music

    }

    private const float defaultMinDistance = 5f;
    private const float defaultMaxDistance = 50f;
    private const bool looping = false;
    private AudioSource _musicSource;

    public override void Awake() {
        base.Awake();
        EnsureMusicSource();
    }

    /**
     * Plays a sound on the supplied Audiosource with our settings so
     * we don't have to set them on every single audiosorce.
     */
    public void PlaySound(AudioSource source, AudioClip clip, AudioGroup group, float pitch, float minDistance = defaultMinDistance, float maxDistance = defaultMaxDistance, bool loop = looping) {
        source.clip = clip;

        ApplyAudioSourceSettings(source, group, pitch, minDistance, maxDistance, loop);

        source.Play();
    }

    /**
     * Plays a sound on the supplied Audiosource with our settings so
     * we don't have to set them on every single audiosorce.
     */
    public void PlaySoundOneShot(AudioSource source, AudioClip clip, AudioGroup group, float pitch = 1f) {
        ApplyAudioSourceSettings(source, group, pitch);

        source.PlayOneShot(clip);
    }

    /**
     * Creates a gameobject with an audiosource, plays the clip and the destroys the game object.
     * Useful for projectiles playing and explode sound or something and the projectile is
     * destroyed before the sound can play or has finished playing.
     */
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, AudioGroup group, float pitch = 1f) {
        GameObject go = new GameObject();
        go.name = "PlaySoundAtPosition";
        go.transform.position = position;

        go.AddComponent<AudioSource>();
        go.GetComponent<AudioSource>().clip = clip;

        ApplyAudioSourceSettings(go.GetComponent<AudioSource>(), group, pitch);

        go.GetComponent<AudioSource>().Play();

        Destroy(go, clip.length);
    }

    public void PlayStageMusic(int stageIndex) {
        AudioClip clip = GetStageMusicClip(stageIndex);
        if (clip == null) {
            return;
        }

        EnsureMusicSource();
        if (_musicSource == null) {
            return;
        }

        if (_musicSource.clip == clip && _musicSource.isPlaying) {
            return;
        }

        _musicSource.clip = clip;
        ApplyAudioSourceSettings(_musicSource, AudioGroup.Music, 1f, defaultMinDistance, defaultMaxDistance, true);
        _musicSource.spatialBlend = 0f;
        _musicSource.volume = 1f;
        _musicSource.Play();
    }

    public void StopMusic() {
        if (_musicSource == null) {
            return;
        }

        _musicSource.Stop();
        _musicSource.clip = null;
    }

    private void ApplyAudioSourceSettings(AudioSource source, AudioGroup group, float pitch = 1f, float minDistance = defaultMinDistance, float maxDistance = defaultMaxDistance, bool loop = looping) {
        source.pitch = pitch;
        source.dopplerLevel = 0;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.loop = loop;
        source.playOnAwake = false;
        source.rolloffMode = AudioRolloffMode.Logarithmic;

        switch (group) {
            case AudioGroup.SFX:
                source.outputAudioMixerGroup = sfxGroup;
                break;
            case AudioGroup.Ambient:
                source.outputAudioMixerGroup = ambientGroup;
                break;
            case AudioGroup.Music:
                source.outputAudioMixerGroup = musicGroup;
                break;
        }
    }

    private void EnsureMusicSource() {
        if (_musicSource != null) {
            return;
        }

        _musicSource = GetComponent<AudioSource>();
        if (_musicSource == null) {
            _musicSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private AudioClip GetStageMusicClip(int stageIndex) {
        switch (stageIndex) {
            case 1:
                return stage1Music;
            case 2:
                return stage2Music;
            case 3:
                return stage3Music;
            case 4:
                return stage4Music;
            default:
                return null;
        }
    }

}
