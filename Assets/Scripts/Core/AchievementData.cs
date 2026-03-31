using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAchievementData", menuName = "UGS/Achievement Data")]
public class AchievementData : ScriptableObject
{
    [System.Serializable]
    public class AchievementItem
    {
        public string id;          // ID để mở khóa (Ví dụ: ACH_MODE3_01)
        public string name;        // Tên danh hiệu
        public string description; // Mô tả cách đạt
        public Sprite iconSprite; // GameObject chứa Icon (Sprite, Image hoặc 3D Object nhỏ)
    }

    [Header("Danh sách danh hiệu cho Mode này")]
    public List<AchievementItem> achievements = new List<AchievementItem>();
}