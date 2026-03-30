using UnityEngine;
using UnityEngine.UI;

public class DiceTile : MonoBehaviour
{
    public int tileIndex;
    public bool isClaimed = false; // Manager sẽ dựa vào đây để không mở khóa lại ô đã chọn
    private Image img;
    private Button btn;

    void Awake()
    {
        img = GetComponent<Image>();
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    }

    public void SetVisual(Sprite sp)
    {
        if (img != null && sp != null)
            img.sprite = sp;
    }

    public void SetInteractable(bool state)
    {
        if (btn != null)
            btn.interactable = state;
    }

    private void OnClick()
    {
        if (DiceModeManager.Instance != null)
        {
            DiceModeManager.Instance.OnTileClicked(tileIndex, this);
        }
    }
}