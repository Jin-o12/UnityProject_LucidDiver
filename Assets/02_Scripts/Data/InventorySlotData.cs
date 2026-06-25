using UnityEngine;

/// <summary>
/// 인벤토리 슬롯 1칸이 들고 있는 데이터를 보관한다.
/// TID와 아이콘뿐 아니라 실제 ItemData도 함께 들고 있어야
/// 상자 UI로 아이템을 다시 옮길 때 원본 데이터를 사용할 수 있다.
/// </summary>
[System.Serializable]
public class InventorySlotData
{
    public int TID;             // 아이템 고유 ID (0이면 빈 슬롯)
    public int order;           // 인벤토리 내 슬롯 순서
    public int amount;          // 현재 슬롯 수량
    public Sprite icon;         // UI에 표시할 아이콘
    public ItemData itemData;   // 실제 아이템 원본 데이터

    public InventorySlotData(int _id, int _order, int _amount, Sprite _icon, ItemData _itemData = null)
    {
        TID = _id;
        order = _order;
        amount = _amount;
        icon = _icon;
        itemData = _itemData;
    }
}
