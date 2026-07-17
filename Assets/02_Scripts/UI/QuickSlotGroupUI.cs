using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct slotData
{
    public Image slotIcon;
    public TMP_Text itemStack;
}

public class QuickSlotGroupUI : MonoBehaviour
{
    [Header("무기 UI")]
    [SerializeField] private Image weaponImage;

    [Header("퀵슬롯 아이템 UI")]
    [SerializeField] private QuickSlotUI[] slotDataList;

    private void Awake()
    {
        InitializeSlots();
    }

    private void OnEnable()
    {
        // 퀵슬롯 데이터 변경 이벤트를 받아 UI를 갱신합니다.
        GlobalEventBus.OnQuickSlotChanged += UpdateSlot;
    }

    private void OnDisable()
    {
        // UI가 비활성화될 때 이벤트 중복 구독을 방지합니다.
        GlobalEventBus.OnQuickSlotChanged -= UpdateSlot;
    }

    public void InitializeSlots()
    {
        if (slotDataList == null)
            return;

        for (int i = 0; i < slotDataList.Length; i++)
        {
            if (slotDataList[i] == null)
            {
                Debug.LogWarning($"QuickSlotGroupUI: slotDataList[{i}]가 연결되지 않아 해당 퀵슬롯 초기화를 건너뜁니다.", this);
                continue;
            }

            slotDataList[i].Initialize(i);
        }
    }

    public void SyncFromInventory(PlayerInventory playerInventory)
    {
        if (playerInventory == null || playerInventory.quickSlots == null || slotDataList == null)
            return;

        // GamePlayUI가 생성되기 전에 퀵슬롯 복원 이벤트가 먼저 발생한 경우를 보정하기 위해
        // 현재 PlayerInventory가 들고 있는 퀵슬롯 상태를 UI에 한 번 직접 반영합니다.
        int syncCount = Mathf.Min(slotDataList.Length, playerInventory.quickSlots.Count);
        for (int i = 0; i < syncCount; i++)
        {
            InventorySlotData quickSlot = playerInventory.quickSlots[i];
            if (quickSlot == null || quickSlot.TID == 0 || quickSlot.amount <= 0)
            {
                UpdateSlot(i, null, 0, ItemGrade.empty);
                continue;
            }

            ItemGrade grade = quickSlot.itemData != null ? quickSlot.itemData.itemGrade : ItemGrade.empty;
            UpdateSlot(i, quickSlot.icon, quickSlot.amount, grade);
        }
    }

    public void UpdateSlot(int index, Sprite icon, int count)
    {
        UpdateSlot(index, icon, count, ItemGrade.empty);
    }

    public void UpdateSlot(int index, Sprite icon, int count, ItemGrade grade)
    {
        if (slotDataList == null || index < 0 || index >= slotDataList.Length || slotDataList[index] == null)
        {
            Debug.LogWarning($"QuickSlotGroupUI: 유효하지 않은 퀵슬롯 인덱스입니다. index={index}", this);
            return;
        }

        slotDataList[index].UpdateSlot(count, icon, grade);
    }
}
