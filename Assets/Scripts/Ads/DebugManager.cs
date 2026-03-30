using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugPanelManager : MonoBehaviour
{
    public GameObject debugPanel;

    public void TogglePanel()
    {
        debugPanel.SetActive(!debugPanel.activeSelf);
    }

    public void UnlockAll()
    {
        PlayerPrefs.SetInt("Mode2", 1);
        PlayerPrefs.SetInt("Mode3", 1);
        PlayerPrefs.Save();
        Reload();
    }

    public void BuyRemoveAds()
    {
        PlayerPrefs.SetInt("RemoveAds", 1);
        PlayerPrefs.Save();
        Reload();
    }

    public void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Reload();
    }

    private void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}