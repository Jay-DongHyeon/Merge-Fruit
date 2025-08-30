// Scripts/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip dropClip;
    [SerializeField] private AudioClip mergeClip;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.5f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    // --- BGM ---
    public void PlayBgm() => EnsureBgmPlaying();   // ★ 다른 스크립트 호환용
    public void EnsureBgmPlaying()
    {
        if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.Play();
    }
    public void StopBgm()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }

    // --- SFX ---
    public void PlayDrop()
    {
        if (dropClip == null || sfxSource == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(dropClip, sfxVolume);
    }

    public void PlayMerge()
    {
        if (mergeClip == null || sfxSource == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(mergeClip, sfxVolume);
    }

    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (bgmSource) bgmSource.volume = bgmVolume;
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }
}
