// Scripts/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip dropClip;
    [SerializeField] private AudioClip mergeClip;
    [SerializeField] private AudioClip btnClickClip;
    [SerializeField] private AudioClip GameOverClip;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.5f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private const string KEY_BGM = "VOL_BGM";
    private const string KEY_SFX = "VOL_SFX";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // --- 저장된 볼륨 불러오기 ---
        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, sfxVolume);

        // --- BGM Source ---
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;

        // --- SFX Source ---
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    // --- BGM ---
    public void PlayBgm() => EnsureBgmPlaying();   // 다른 스크립트 호환용
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

    public void PlayClick()
    {
        if(btnClickClip== null || sfxSource == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(btnClickClip, sfxVolume);
    }
    public void PlayGameOver()
    {
        if (GameOverClip == null || sfxSource == null) return;
        Debug.Log("게임오버효과음");
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(GameOverClip, sfxVolume);
    }

    // --- Volume Controls ---
    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (bgmSource) bgmSource.volume = bgmVolume;

        PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        if (sfxSource) sfxSource.volume = sfxVolume;

        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetBgmVolume() => bgmVolume;
    public float GetSfxVolume() => sfxVolume;
}
