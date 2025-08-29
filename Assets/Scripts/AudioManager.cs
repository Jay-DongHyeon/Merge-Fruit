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
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("SFX Variations")]
    [SerializeField, Range(0.5f, 2f)] private float dropPitchMin = 0.95f;
    [SerializeField, Range(0.5f, 2f)] private float dropPitchMax = 1.05f;
    [SerializeField, Range(0.5f, 2f)] private float mergePitchMin = 0.95f;
    [SerializeField, Range(0.5f, 2f)] private float mergePitchMax = 1.05f;

    private AudioSource bgmSource;   // 루프용
    private AudioSource sfxSource;   // 원샷용
    private bool bgmTriedStart = false; // WebGL: 첫 사용자 입력 전 자동 재생 방지

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f; // 2D

        // SFX
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D
    }

    // --- 외부에서 호출하는 API ---
    public void EnsureBgmPlaying()
    {
        if (bgmSource == null || bgmClip == null) return;
        if (!bgmSource.isPlaying)
        {
            // 일부 브라우저/WebGL에선 사용자 입력 이후 재생만 허용
            bgmSource.Play();
            bgmTriedStart = true;
        }
    }

    public void PlayDrop()
    {
        if (dropClip == null) return;
        sfxSource.pitch = Random.Range(dropPitchMin, dropPitchMax);
        sfxSource.PlayOneShot(dropClip, sfxVolume);
    }

    public void PlayMerge()
    {
        if (mergeClip == null) return;
        sfxSource.pitch = Random.Range(mergePitchMin, mergePitchMax);
        sfxSource.PlayOneShot(mergeClip, sfxVolume);
    }

    // --- 옵션: 볼륨/뮤트 제어 ---
    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }

    public void MuteAll(bool mute)
    {
        if (bgmSource) bgmSource.mute = mute;
        if (sfxSource) sfxSource.mute = mute;
    }
}
