using UnityEngine;

public class UGS_AchievementLocal : MonoBehaviour
{
    public GameObject achievementPanel;
    public GameObject itemPrefab;
    public Transform contentContainer;

    [Header("Dữ liệu Mode hiện tại")]
    public AchievementData currentModeData;

    public void OpenPanel()
    {
        achievementPanel.SetActive(true);
        ShowAchievements();
    }

    private void ShowAchievements()
    {
        if (currentModeData == null || itemPrefab == null || contentContainer == null) return;

        // Xóa các ô cũ
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // Sinh ra danh hiệu từ Sprite trong Data
        foreach (var ach in currentModeData.achievements)
        {
            bool isUnlocked = PlayerPrefs.GetInt(ach.id, 0) == 1;

            GameObject newItem = Instantiate(itemPrefab, contentContainer);
            AchievementItemUI uiScript = newItem.GetComponent<AchievementItemUI>();

            if (uiScript != null)
            {
                uiScript.Setup(ach.name, ach.description, ach.iconSprite, isUnlocked);
            }
        }
    }

    public static void Unlock(string achId)
    {
        if (PlayerPrefs.GetInt(achId, 0) == 0)
        {
            PlayerPrefs.SetInt(achId, 1);
            PlayerPrefs.Save();
            Debug.Log($"<color=yellow>[Achievement]</color> Unlocked: {achId}");
        }
    }

    public void ClosePanel() => achievementPanel.SetActive(false);
}