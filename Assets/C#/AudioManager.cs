using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("音频Clip")]
    public AudioClip bgmClip;
    public AudioClip appleClip;
    public AudioClip bananaClip;
    public AudioClip melonClip;
    public AudioClip bananaPeelClip;
    public AudioClip bombClip;
    public AudioClip resultClip;

    [Header("音量")]
    [Range(0, 1)] public float bgmVolume = 0.4f;
    [Range(0, 1)] public float sfxVolume = 0.7f;

    private AudioSource bgmSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;
    }

    /// <summary>播放背景音乐（对局开始）</summary>
    public void PlayBGM()
    {
        if (bgmClip == null) return;
        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    /// <summary>停止背景音乐（结算）</summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>播放一次性音效</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, sfxVolume);
    }

    //快捷调用
    public void PlayApple() => PlaySFX(appleClip);
    public void PlayBanana() => PlaySFX(bananaClip);
    public void PlayMelon() => PlaySFX(melonClip);
    public void PlayBananaPeel() => PlaySFX(bananaPeelClip);
    public void PlayBomb() => PlaySFX(bombClip);
    public void PlayResult() => PlaySFX(resultClip);
}
