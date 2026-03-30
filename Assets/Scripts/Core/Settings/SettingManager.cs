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
    public Image vibrateBtnImage; // Kéo Image của nút Rung vào đây
    public Sprite vibrateOnSprite; // Sprite khi bật rung
    public Sprite vibrateOffSprite; // Sprite khi tắt rung

    void Start()
    {
        // 1. Load giá trị từ GlobalSettings (đã bao gồm PlayerPrefs)
        musicSlider.value = GlobalSettings.MusicVolume;
        sfxSlider.value = GlobalSettings.SFXVolume;

        // 2. Đăng ký sự kiện thay đổi giá trị cho Slider
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

        // 3. Cập nhật giao diện và âm lượng Mixer ban đầu
        UpdateVisuals();
        SyncMixerVolumes();
    }

    private void SyncMixerVolumes()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.mainMixer != null)
        {
            AudioManager.Instance.mainMixer.SetFloat("MusicVol", GlobalSettings.SliderToDecibel(musicSlider.value));
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

    // Hàm xử lý khi bấm vào nút Rung (Gán vào OnClick của Button Rung)
    public void ToggleVibrate()
    {
        GlobalSettings.IsVibrate = !GlobalSettings.IsVibrate;

        if (GlobalSettings.IsVibrate)
        {
            GlobalSettings.PlayVibrate(); // Rung thử một cái khi bật
        }

        UpdateVisuals();
    }

    public void ToggleMusic()
    {
        musicSlider.value = (musicSlider.value > 0) ? 0 : 0.75f;
    }

    public void ToggleSFX()
    {
        sfxSlider.value = (sfxSlider.value > 0) ? 0 : 0.75f;
    }

    private void UpdateVisuals()
    {
        // Cập nhật Sprite cho loa Nhạc
        if (musicBtnImage != null)
            musicBtnImage.sprite = (musicSlider.value > 0) ? musicOnSprite : musicOffSprite;

        // Cập nhật Sprite cho loa SFX
        if (sfxBtnImage != null)
            sfxBtnImage.sprite = (sfxSlider.value > 0) ? sfxOnSprite : sfxOffSprite;

        // Cập nhật Sprite cho nút Rung
        if (vibrateBtnImage != null)
            vibrateBtnImage.sprite = GlobalSettings.IsVibrate ? vibrateOnSprite : vibrateOffSprite;
    }

    public void CloseSetting()
    {
        PlayerPrefs.Save();

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            // Tắt Panel
            this.gameObject.SetActive(false);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }
}