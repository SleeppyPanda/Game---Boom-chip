using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingManager : MonoBehaviour
{
    [Header("Âm thanh (Music)")]
    public Slider musicSlider;
    public Image musicBtnImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    [Header("Hiệu ứng (SFX)")]
    public Slider sfxSlider;
    public Image sfxBtnImage;
    public Sprite sfxOnSprite;
    public Sprite sfxOffSprite;

    [Header("Rung (Vibration)")]
    public Image vibrateBtnImage;
    public Sprite vibrateOnSprite;
    public Sprite vibrateOffSprite;

    [Header("Privacy Policy Panel")]
    public GameObject privacyPolicyPanel; 

    void Start()
    {
        if (musicSlider != null) musicSlider.value = GlobalSettings.MusicVolume;
        if (sfxSlider != null) sfxSlider.value = GlobalSettings.SFXVolume;

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

        UpdateVisuals();
        SyncMixerVolumes();

        if (privacyPolicyPanel != null) privacyPolicyPanel.SetActive(false);
    }

    private void SyncMixerVolumes()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.mainMixer != null)
        {
            if (musicSlider != null)
                AudioManager.Instance.mainMixer.SetFloat("MusicVol", GlobalSettings.SliderToDecibel(musicSlider.value));

            if (sfxSlider != null)
                AudioManager.Instance.mainMixer.SetFloat("SFXVol", GlobalSettings.SliderToDecibel(sfxSlider.value));
        }
    }

    private void OnMusicSliderChanged(float value)
    {
        GlobalSettings.MusicVolume = value;
        if (AudioManager.Instance != null && AudioManager.Instance.mainMixer != null)
        {
            AudioManager.Instance.mainMixer.SetFloat("MusicVol", GlobalSettings.SliderToDecibel(value));
        }
        UpdateVisuals();
    }

    private void OnSFXSliderChanged(float value)
    {
        GlobalSettings.SFXVolume = value;
        if (AudioManager.Instance != null && AudioManager.Instance.mainMixer != null)
        {
            AudioManager.Instance.mainMixer.SetFloat("SFXVol", GlobalSettings.SliderToDecibel(value));
        }
        UpdateVisuals();
    }

    public void ToggleVibrate()
    {
        GlobalSettings.IsVibrate = !GlobalSettings.IsVibrate;
        if (GlobalSettings.IsVibrate)
        {
            GlobalSettings.PlayVibrate();
        }
        UpdateVisuals();
        PlayerPrefs.SetInt("VibrateEnable", GlobalSettings.IsVibrate ? 1 : 0);
    }

    public void OpenPrivacyPolicy()
    {
        if (privacyPolicyPanel != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");
            privacyPolicyPanel.SetActive(true);
        }
    }

    public void ClosePrivacyPolicy()
    {
        if (privacyPolicyPanel != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");
            privacyPolicyPanel.SetActive(false);
        }
    }


    public void ToggleMusic()
    {
        if (musicSlider != null)
            musicSlider.value = (musicSlider.value > 0) ? 0 : 0.75f;
    }

    public void ToggleSFX()
    {
        if (sfxSlider != null)
            sfxSlider.value = (sfxSlider.value > 0) ? 0 : 0.75f;
    }

    private void UpdateVisuals()
    {
        if (musicBtnImage != null && musicSlider != null)
            musicBtnImage.sprite = (musicSlider.value > 1e-4) ? musicOnSprite : musicOffSprite;

        if (sfxBtnImage != null && sfxSlider != null)
            sfxBtnImage.sprite = (sfxSlider.value > 1e-4) ? sfxOnSprite : sfxOffSprite;

        if (vibrateBtnImage != null)
        {
            vibrateBtnImage.sprite = GlobalSettings.IsVibrate ? vibrateOnSprite : vibrateOffSprite;
        }
    }

    public void CloseSetting()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");
        PlayerPrefs.Save();
        this.gameObject.SetActive(false);
    }
}