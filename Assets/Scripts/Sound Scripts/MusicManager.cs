using UnityEngine;

// Plays background music and remembers the volume.
// - Main menu: menu track
// - Game: game track, switching to the boss track in the boss round
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    const string VolumeKey = "MusicVolume";

    [Header("Tracks")]
    public AudioClip menuTrack;
    public AudioClip gameTrack;
    public AudioClip bossTrack;

    [Header("Settings")]
    [Tooltip("Volume the very first time the game is run (0 to 1)")]
    [Range(0f, 1f)] public float defaultVolume = 0.6f;

    [Tooltip("Seconds taken to fade out one track before the next one starts")]
    public float fadeTime = 1.5f;

    public float Volume { get; private set; }

    private AudioSource source;
    private float fade = 1f;

    private void Awake()
    {
        // Only one MusicManager can exist. If a second one loads with a scene, it removes itself
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        Volume = PlayerPrefs.GetFloat(VolumeKey, defaultVolume);
        source.volume = Volume;
    }

    private void Update()
    {
        AudioClip target = menuTrack;

        if (WaveManager.Instance != null)
        {
            bool boss = WaveManager.Instance.IsBossRound && bossTrack != null;
            target = boss ? bossTrack : gameTrack;
        }

        // Unscaled time, so the fade still works while the game is paused
        if (target != source.clip)
        {
            fade -= Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime);

            if (fade <= 0f)
            {
                fade = 0f;
                source.clip = target;

                if (target != null) source.Play();
                else source.Stop();
            }
        }
        else if (fade < 1f)
        {
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
        }

        source.volume = Volume * fade;
    }

    // Called by the volume sliders
    public void SetVolume(float value)
    {
        Volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, Volume);
    }
}