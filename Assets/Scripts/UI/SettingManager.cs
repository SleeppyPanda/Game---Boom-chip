using UnityEngine;
using UnityEngine.UI;

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

    // --- PHẦN BỔ SUNG: Nút quay lại ---
    public void CloseSetting()
    {
        // Bạn có thể đơn giản là tắt Object này đi
        // Hoặc nếu muốn mượt mà, hãy dùng CanvasGroup để Fade Out (nếu panelSetting có CanvasGroup)
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            // Sử dụng DOTween để ẩn mượt mà giống MenuManager
            // Hoặc đơn giản là tắt ngay lập tức:
            this.gameObject.SetActive(false);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // Load giá trị cũ
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        // Lắng nghe sự kiện thay đổi
        musicSlider.onValueChanged.AddListener(delegate { OnSliderChanged(musicSlider, musicBtnImage, musicOnSprite, musicOffSprite, "MusicVolume"); });
        sfxSlider.onValueChanged.AddListener(delegate { OnSliderChanged(sfxSlider, sfxBtnImage, sfxOnSprite, sfxOffSprite, "SFXVolume"); });

        // Cập nhật Sprite ban đầu
        UpdateButtonSprite(musicSlider.value, musicBtnImage, musicOnSprite, musicOffSprite);
        UpdateButtonSprite(sfxSlider.value, sfxBtnImage, sfxOnSprite, sfxOffSprite);
    }

    private void OnSliderChanged(Slider slider, Image img, Sprite onS, Sprite offS, string saveKey)
    {
        PlayerPrefs.SetFloat(saveKey, slider.value);
        UpdateButtonSprite(slider.value, img, onS, offS);
    }

    public void ToggleMusic() { ToggleMute(musicSlider); }
    public void ToggleSFX() { ToggleMute(sfxSlider); }

    private void ToggleMute(Slider slider)
    {
        slider.value = (slider.value > 0) ? 0 : 0.5f;
    }

    private void UpdateButtonSprite(float value, Image img, Sprite onS, Sprite offS)
    {
        if (img == null) return;
        img.sprite = (value > 0) ? onS : offS;
    }
}