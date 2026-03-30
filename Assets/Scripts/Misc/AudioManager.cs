using TwiiK.Utility;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : Singleton<AudioManager> {

    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ambientGroup;
    public AudioMixerGroup musicGroup;

    [Header("Stage Music")]
    [SerializeField] private AudioClip stage1Music;
    [SerializeField] private AudioClip stage2Music;
    [SerializeField] private AudioClip stage3Music;
    [SerializeField] private AudioClip stage4Music;

    [Header("Cursor")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = new Vector2(282f, 232f);
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Header("UI")]
    [SerializeField] private GameObject settingsUIPrefab;

    public enum AudioGroup {

        SFX,
        Ambient,
        Music

    }

    private const float defaultMinDistance = 5f;
    private const float defaultMaxDistance = 50f;
    private const bool looping = false;
    private const string MasterVolumeKey = "settings.audio.master";
    private const string BackgroundVolumeKey = "settings.audio.background";
    private const string SfxVolumeKey = "settings.audio.sfx";
    private AudioSource _musicSource;

    public GameObject SettingsUIPrefab => settingsUIPrefab;

    public override void Awake() {
        base.Awake();
        EnsureMusicSource();
        ApplySavedVolumeSettings();
        ApplyCursor();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        if (Instance == this) {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
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
        if (source == null) {
            return;
        }

        source.pitch = pitch;
        source.dopplerLevel = 0;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.loop = loop;
        source.playOnAwake = false;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.volume = GetGroupVolume(group);

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

    public void ConfigureSource(AudioSource source, AudioGroup group, float pitch = 1f, bool loop = false) {
        ApplyAudioSourceSettings(source, group, pitch, defaultMinDistance, defaultMaxDistance, loop);
    }

    public void ApplySavedVolumeSettings() {
        AudioListener.volume = GetSavedMasterVolume();
        EnsureMusicSource();

        if (_musicSource != null) {
            ApplyAudioSourceSettings(_musicSource, AudioGroup.Music, _musicSource.pitch, _musicSource.minDistance, _musicSource.maxDistance, _musicSource.loop);
            _musicSource.spatialBlend = 0f;
        }

        RefreshSceneAudioSources();
    }

    public static float GetSavedMasterVolume() {
        return PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
    }

    public static float GetSavedBackgroundVolume() {
        return PlayerPrefs.GetFloat(BackgroundVolumeKey, 1f);
    }

    public static float GetSavedSfxVolume() {
        return PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    public static void SetSavedMasterVolume(float value) {
        SaveVolume(MasterVolumeKey, value);
        AudioListener.volume = Mathf.Clamp01(value);

        if (Instance != null) {
            Instance.ApplySavedVolumeSettings();
        }
    }

    public static void SetSavedBackgroundVolume(float value) {
        SaveVolume(BackgroundVolumeKey, value);

        if (Instance != null) {
            Instance.ApplySavedVolumeSettings();
        }
    }

    public static void SetSavedSfxVolume(float value) {
        SaveVolume(SfxVolumeKey, value);

        if (Instance != null) {
            Instance.ApplySavedVolumeSettings();
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

    private float GetGroupVolume(AudioGroup group) {
        switch (group) {
            case AudioGroup.Music:
            case AudioGroup.Ambient:
                return GetSavedBackgroundVolume();
            case AudioGroup.SFX:
            default:
                return GetSavedSfxVolume();
        }
    }

    private void RefreshSceneAudioSources() {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++) {
            AudioSource source = audioSources[i];
            if (source == null || source == _musicSource) {
                continue;
            }

            AudioGroup group = ResolveGroup(source);
            source.volume = GetGroupVolume(group);

            if (group == AudioGroup.SFX && sfxGroup != null) {
                source.outputAudioMixerGroup = sfxGroup;
            }
            else if (group == AudioGroup.Ambient && ambientGroup != null) {
                source.outputAudioMixerGroup = ambientGroup;
            }
            else if (group == AudioGroup.Music && musicGroup != null) {
                source.outputAudioMixerGroup = musicGroup;
            }
        }
    }

    private AudioGroup ResolveGroup(AudioSource source) {
        if (source == _musicSource) {
            return AudioGroup.Music;
        }

        if (source.outputAudioMixerGroup == ambientGroup) {
            return AudioGroup.Ambient;
        }

        if (source.outputAudioMixerGroup == musicGroup) {
            return AudioGroup.Music;
        }

        return AudioGroup.SFX;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        ApplySavedVolumeSettings();
        ApplyCursor();
    }

    private void ApplyCursor() {
        if (cursorTexture == null) {
            return;
        }

        Cursor.SetCursor(cursorTexture, cursorHotspot, cursorMode);
        Cursor.visible = true;
    }

    private static void SaveVolume(string key, float value) {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
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
