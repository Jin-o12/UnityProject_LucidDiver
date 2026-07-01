/// <summary>
/// 퀵슬롯 입력과 아이템 사용 효과를 중재하는 프레젠터입니다.
/// 퀵슬롯에 등록된 아이템이 실제로 소비된 경우에만 효과를 실행합니다.
/// </summary>
using UnityEngine;

public class QuickSlotPresenter : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    public PlayerInventory inventory;

    // 아이템 데이터 조회 인터페이스
    private IItemDataRepository itemRepo;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            enabled = false;
            Debug.LogError("QuickSlotPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }

        itemRepo = new SOItemRepository();
    }

    private void OnEnable()
    {
        GlobalEventBus.OnQuickSlotUseRequested += UseQuickSlotItem;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnQuickSlotUseRequested -= UseQuickSlotItem;
    }

    /* 퀵슬롯에 등록된 아이템을 사용 */
    private void UseQuickSlotItem(int slotIndex)
    {
        // 퀵슬롯 범위 및 등록 여부 확인
        if (slotIndex < 0 || slotIndex >= inventory.quickSlotNum) return;

        InventorySlotData slot = inventory.quickSlots[slotIndex];
        if (slot == null || slot.TID == 0 || slot.amount <= 0) return;

        ItemData itemData = slot.itemData ?? itemRepo.GetItemData(slot.TID);
        if (itemData == null) return;

        // 실제 인벤토리에서 소비가 성공했을 때만 아이템 효과를 실행합니다.
        bool isConsumed = inventory.UseQuickSlotItem(slotIndex);
        if (!isConsumed) return;

        ConsumeItemData consumable = itemData as ConsumeItemData;
        if (consumable == null) return;

        for (int i = 0; i < consumable.useEffect.Count; i++)
        {
            ItemEffect currentEffect = consumable.useEffect[i];
            GameObject targetObj = null;

            switch (currentEffect.effectTarget)
            {
                case EffectTarget.self:
                    targetObj = gameObject;
                    break;
                case EffectTarget.enemy:
                    Debug.Log($"적에게 사용하는 효과는 아직 연결되지 않았습니다: {itemData.itemName}");
                    break;
            }

            if (targetObj != null)
            {
                currentEffect.Execute(targetObj);
            }
        }
    }
}
