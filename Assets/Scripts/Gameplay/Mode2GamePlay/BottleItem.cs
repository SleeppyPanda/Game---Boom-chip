using UnityEngine;

public class BottleItem : MonoBehaviour
{
    [Tooltip("ID của loại chai này")]
    public int ID;

    public void Setup(int newID)
    {
        ID = newID;
    }
}