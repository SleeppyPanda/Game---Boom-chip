using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;
using DG.Tweening; // Cần có DOTween để dùng hiệu ứng mượt mà

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer Setup")]
    public AudioMixer mainMixer;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips Library")]
    public List<AudioClip> musicClips;
    public List<AudioClip> sfxClips;

    private Dictionary<string, AudioClip> musicDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitDictionaries();
        }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        ApplyMixerVolume();
    }

    void InitDictionaries()
    {
        foreach (var clip in musicClips) if (clip != null) musicDict[clip.name] = clip;
        foreach (var clip in sfxClips) if (clip != null) sfxDict[clip.name] = clip;
    }

    public void ApplyMixerVolume()
    {
        mainMixer.SetFloat("MusicVol", GlobalSettings.SliderToDecibel(GlobalSettings.MusicVolume));
        mainMixer.SetFloat("SFXVol", GlobalSettings.SliderToDecibel(GlobalSettings.SFXVolume));
    }

    public void PlayMusic(string clipName)
    {
        if (musicDict.TryGetValue(clipName, out AudioClip clip))
        {
            // Nếu nhạc đang phát chính là bài này rồi thì không làm gì cả
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            // Dùng Coroutine hoặc DOTween để chuyển nhạc mượt mà
            StopAllCoroutines();
            StartCoroutine(CrossFadeMusic(clip));
        }
        else
        {
            Debug.LogWarning("AudioManager: Không tìm thấy nhạc có tên: " + clipName);
        }
    }

    private System.Collections.IEnumerator CrossFadeMusic(AudioClip newClip)
    {
        float duration = 0.5f; // Thời gian chuyển nhạc
        float targetVolume = 1.0f; // Bạn có thể lấy volume mặc định từ settings

        // 1. Nhạc cũ nhỏ dần
        if (musicSource.isPlaying)
        {
            float currentTime = 0;
            float startVol = musicSource.volume;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0, currentTime / duration);
                yield return null;
            }
        }

        // 2. Đổi clip và phát nhạc mới
        musicSource.clip = newClip;
        musicSource.Play();

        // 3. Nhạc mới to dần
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, targetVolume, t / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void PlaySFX(string clipName)
    {
        if (sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}