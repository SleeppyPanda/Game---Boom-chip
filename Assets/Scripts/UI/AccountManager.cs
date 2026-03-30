using UnityEngine;
using System;
using System.Collections.Generic;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance;

    // Sự kiện để các UnlockButton tự làm mới giao diện khi mở khóa thành công
    public static Action OnAvatarUnlocked;

    [Header("Dữ liệu Avatar (Nguồn duy nhất)")]
    public List<Sprite> allAvatars;

    [Header("Trạng thái hiện tại")]
    public int currentPlayerID = 1; // 1 cho P1, 2 cho P2

    // Biến static lưu kết quả để các scene khác (Win Panel) truy cập ngay lập tức
    public static int LastWinnerID = 1;

    // Keys PlayerPrefs (Dùng chung cho toàn game)
    private const string NAME_BASE_KEY = "PlayerName";
    private const string AVATAR_BASE_KEY = "SelectedAvatarIndex";
    private const string UNLOCK_PREFIX = "Unlock_Avatar_";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeData()
    {
        // Mặc định luôn mở khóa Avatar index 0 cho người chơi mới
        if (PlayerPrefs.GetInt(UNLOCK_PREFIX + "0", 0) == 0)
        {
            PlayerPrefs.SetInt(UNLOCK_PREFIX + "0", 1);
            PlayerPrefs.Save();
        }
    }

    void OnDestroy()
    {
        // Dọn dẹp sự kiện khi object bị hủy để tránh lỗi rò rỉ bộ nhớ
        OnAvatarUnlocked = null;
    }

    // --- HÀM TRUY XUẤT DỮ LIỆU ---

    public string GetPlayerName(int id) => PlayerPrefs.GetString(NAME_BASE_KEY + "_P" + id, "Player " + id);

    public int GetAvatarIndex(int id) => PlayerPrefs.GetInt(AVATAR_BASE_KEY + "_P" + id, (id == 1) ? 0 : 0);

    public Sprite GetAvatarSprite(int id)
    {
        int index = GetAvatarIndex(id);
        if (allAvatars != null && index >= 0 && index < allAvatars.Count) return allAvatars[index];
        return null;
    }

    public bool IsAvatarUnlocked(int index) => PlayerPrefs.GetInt(UNLOCK_PREFIX + index, 0) == 1;

    // --- HÀM THAY ĐỔI DỮ LIỆU ---

    public void SwitchEditingPlayer(int id) => currentPlayerID = id;

    /// <summary>
    /// Được gọi bởi UnlockButton khi người dùng nhấn chọn Avatar
    /// </summary>
    public void SelectAvatar(int index)
    {
        // Kiểm tra xem Avatar này đã mở khóa chưa trước khi cho phép chọn
        if (IsAvatarUnlocked(index))
        {
            SaveAvatarIndex(currentPlayerID, index);
            Debug.Log($"<color=cyan>Player {currentPlayerID} changed avatar to index {index}</color>");

            // Nếu bạn có AccountUIHandler, hãy gọi RefreshUI tại đây hoặc qua một Event khác
            AccountUIHandler uiHandler = FindObjectOfType<AccountUIHandler>();
            if (uiHandler != null) uiHandler.RefreshUI();
        }
        else
        {
            Debug.LogWarning("Avatar này chưa được mở khóa!");
        }
    }

    public void SavePlayerName(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        PlayerPrefs.SetString(NAME_BASE_KEY + "_P" + id, name.Trim());
        PlayerPrefs.Save();
    }

    public void SaveAvatarIndex(int id, int index)
    {
        PlayerPrefs.SetInt(AVATAR_BASE_KEY + "_P" + id, index);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Hàm gọi khi UnlockSystemManager xác nhận mở khóa thành công (sau khi xem Ads)
    /// </summary>
    public void UnlockAvatar(int index)
    {
        PlayerPrefs.SetInt(UNLOCK_PREFIX + index, 1);
        PlayerPrefs.Save();

        // Kích hoạt sự kiện để các UnlockButton trên màn hình tự ẩn Lock Icon
        OnAvatarUnlocked?.Invoke();

        Debug.Log($"<color=green>Unlocked Avatar index {index} thành công!</color>");
    }

    /// <summary>
    /// Hàm lưu dữ liệu cuối cùng khi thoát Panel (đảm bảo an toàn dữ liệu)
    /// </summary>
    public void SaveAndExit()
    {
        PlayerPrefs.Save();
    }

    public static void SetWinResult(int winnerID) => LastWinnerID = winnerID;
}