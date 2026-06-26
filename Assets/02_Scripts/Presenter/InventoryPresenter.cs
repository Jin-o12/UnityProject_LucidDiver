/// <summary>
/// 아이템과 인벤토리에 관한 모든 상호작용의 중재자 역할을 수행한다.
/// 아이템 습득, 인벤토리 UI 열기/닫기, 체스트 UI 연결, 월드 드랍 처리를 담당한다.
/// </summary>
using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    // 게임 데이터 스크립트 참조
    private PlayerWeapon playerWeapon;               // 플레이어 무기
    private PlayerInventory playerInventory;         // 플레이어 인벤토리
    private LocalInputReader localInputReader;       // 플레이어 입력 처리
    private EntityIdentity identity;                 // 플레이어 고유 ID
    private PlayerStatus playerStatus;               // 플레이어 상태

    // 컨테이너 상태 캐시
    private ItemBox currentBox;                      // 현재 열려 있는 상자
    private bool isChestOpen;                        // 체스트 UI가 열려 있는지 여부

    // UI 캐시
    private InventoryUI inventoryUI;                 // 인벤토리 UI 캐시
    private ChestUI chestUI;                         // 체스트 UI 캐시
    private ResultUI resultUI;                       // 결과창 UI 캐시

    private const float dropForwardDistance = 1.0f; // 플레이어 앞에 떨어질 거리
    private const float dropUpOffset = 0.3f;        // 바닥 겹침 방지용 높이 보정

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerInventory = GetComponent<PlayerInventory>();
        playerStatus = GetComponent<PlayerStatus>();
        localInputReader = GetComponent<LocalInputReader>();
        identity = GetComponent<EntityIdentity>();
        playerStatus = GetComponent<PlayerStatus>();

        if (playerWeapon == null || playerInventory == null || localInputReader == null || identity == null || playerStatus == null)
        {
            this.enabled = false;
            Debug.LogError("InventoryPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        // 바닥에서 아이템을 주웠을 때 발생하는 이벤트를 구독한다.
        GlobalEventBus.OnItemPickedUp += HandleItemPickUp;

        // 상자와 상호작용했을 때 체스트 UI를 열기 위한 이벤트를 구독한다.
        GlobalEventBus.OnItemBoxOpened += HandleItemBoxOpened;

        // 인벤토리 아이템 버리기 요청 이벤트를 구독한다.
        GlobalEventBus.OnInventoryDropRequested += HandleInventoryDropRequested;

        // 입력으로 인벤토리 열기/닫기 요청이 들어오면 대응한다.
        localInputReader.OnInventoryOpenRequested += OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested += CloseInventoryUI;

        // 인벤토리 슬롯 데이터 변경을 구독한다.
        playerInventory.OnSlotChanged += HandleSlotChanged;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        GlobalEventBus.OnItemPickedUp -= HandleItemPickUp;
        GlobalEventBus.OnItemBoxOpened -= HandleItemBoxOpened;
        GlobalEventBus.OnInventoryDropRequested -= HandleInventoryDropRequested;

        localInputReader.OnInventoryOpenRequested -= OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested -= CloseInventoryUI;

        playerInventory.OnSlotChanged -= HandleSlotChanged;
    }

    /// <summary>
    /// 아이템 줍기 이벤트를 받았을 때 자동 장착 또는 인벤토리 수납을 처리한다.
    /// 인벤토리에 다 못 들어간 수량은 바닥 아이템에 그대로 남긴다.
    /// </summary>
    private void HandleItemPickUp(int pickerID, int pickedItemTID, int count, IInteractable sourceInteractable)
    {
        // 이 획득 이벤트를 발생시킨 실제 바닥 아이템 참조
        DropItem sourceDrop = sourceInteractable as DropItem;

        ItemData data = DataManager.Instance.GetItemData(pickedItemTID);
        if (data == null)
            return;

        // 기본값은 "아직 아무것도 못 주웠다"로 시작한다.
        int remain = count;

        // TID 100대는 무기
        if (100 < pickedItemTID && pickedItemTID < 200)
        {
            // 무기를 장착하지 않은 상태라면 바로 장착한다.
            if (!playerWeapon.isEquipped)
            {
                WeaponItemData weaponData = data as WeaponItemData;

                if (weaponData != null)
                {
                    playerWeapon.EquipWeapon(weaponData);
                    remain = 0;
                }
                else
                {
                    Debug.LogError("무기가 아닌 아이템이 잘못된 TID를 가지고 있습니다.");
                }
            }
            else
            {
                // 이미 무기를 들고 있다면 인벤토리에 넣고,
                // 남은 수량은 바닥에 유지한다.
                remain = playerInventory.AddItem(data, count);
            }
        }
        // TID 300대는 소모품
        else if (300 < pickedItemTID && pickedItemTID < 400)
        {
            remain = playerInventory.AddItem(data, count);
        }
        // TID 400대는 파밍 아이템
        else if (400 < pickedItemTID && pickedItemTID < 500)
        {
            remain = playerInventory.AddItem(data, count);
        }
        // 그 외 아이템은 알 수 없는 아이템
        else
        {
            Debug.LogWarning("Unknown item TID: " + pickedItemTID);
            return;
        }

        // 실제로 몇 개를 주웠는지 결과를 보고 바닥 아이템 상태를 갱신한다.
        UpdateGroundItemAfterPickup(sourceDrop, count, remain);

        DataManager.Instance.SaveGame();
    }

    /// <summary>
    /// 아이템 획득 결과에 따라 바닥 아이템을 삭제하거나,
    /// 남은 수량만 유지하도록 갱신한다.
    /// </summary>
    private void UpdateGroundItemAfterPickup(DropItem sourceDrop, int originalCount, int remain)
    {
        if (sourceDrop == null)
            return;

        // 전부 주운 경우: 바닥 아이템 삭제
        if (remain <= 0)
        {
            Destroy(sourceDrop.gameObject);
            return;
        }

        // 하나도 못 주운 경우: 바닥 아이템 그대로 유지
        if (remain >= originalCount)
            return;

        // 일부만 주운 경우: 남은 수량만 바닥에 유지
        sourceDrop.stackCount = remain;
    }

    /// <summary>
    /// 상자와 상호작용했을 때 인벤토리 UI와 체스트 UI를 함께 연다.
    /// </summary>
    private void HandleItemBoxOpened(IInteractable interactable, int playerID)
    {
        // 다른 플레이어가 연 상자면 무시한다.
        if (identity.entityID != playerID)
            return;

        ItemBox box = interactable as ItemBox;
        if (box == null)
            return;

        currentBox = box;
        isChestOpen = true;

        // 체스트를 여는 동안 입력을 UI 모드로 전환한다.
        localInputReader.SwitchToUIMap();

        // 왼쪽 인벤토리 UI를 연다.
        OpenInventoryUI();

        // 오른쪽 체스트 UI를 열고 데이터와 닫기 콜백을 연결한다.
        chestUI = UIManager.Instance.Open<ChestUI>();
        if (chestUI == null)
            return;

        chestUI.Bind(box, playerInventory, CloseContainerUI);
    }

    /// <summary>
    /// 특정 인벤토리 슬롯이 바뀌었을 때 UI의 그 슬롯만 갱신한다.
    /// </summary>
    private void HandleSlotChanged(int index)
    {
        if (inventoryUI == null || !inventoryUI.gameObject.activeInHierarchy)
            return;

        inventoryUI.UpdateSlot(index, playerInventory.slots[index]);
    }

    /// <summary>
    /// 인벤토리 UI를 연다.
    /// 일반 인벤토리일 때만 드랍존을 사용 가능하게 둔다.
    /// </summary>
    public void OpenInventoryUI()
    {
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        inventoryUI = UIManager.Instance.Open<InventoryUI>();
        if (inventoryUI == null)
            return;

        // 입력 처리 쪽에도 인벤토리 열림 상태를 알려 준다.
        // 일반 인벤토리는 Player 맵을 유지하므로 이 값으로 공격/상호작용을 막는다.
        localInputReader.SetInventoryOpenState(true);

        inventoryUI.SetDropZoneAvailable(!isChestOpen);
        inventoryUI.CreatSlots(playerInventory.slotNum);

        for (int i = 0; i < playerInventory.slotNum; i++)
            inventoryUI.UpdateSlot(i, playerInventory.slots[i]);
    }

    /// <summary>
    /// 일반 인벤토리 UI를 닫는다.
    /// 체스트가 열려 있는 경우에는 체스트 UI까지 함께 닫는다.
    /// </summary>
    public void CloseInventoryUI()
    {
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        if (isChestOpen)
        {
            CloseContainerUI();
            return;
        }

        UIManager.Instance.Close<InventoryUI>();
        inventoryUI = null;

        // 일반 인벤토리를 닫았으니 입력 상태도 원래대로 되돌린다.
        localInputReader.SetInventoryOpenState(false);
        localInputReader.SwitchToPlayerMap();
    }

    /// <summary>
    /// 체스트 UI와 인벤토리 UI를 함께 닫는다.
    /// </summary>
    public void CloseContainerUI()
    {
        if (currentBox != null)
            currentBox.CloseBox();

        UIManager.Instance.Close<ChestUI>();
        UIManager.Instance.Close<InventoryUI>();

        chestUI = null;
        inventoryUI = null;
        currentBox = null;
        isChestOpen = false;

        // 체스트 UI와 인벤토리를 모두 닫았으니 인벤토리 열림 상태를 해제한다.
        localInputReader.SetInventoryOpenState(false);
        localInputReader.SwitchToPlayerMap();
    }

    /// <summary>
    /// 인벤토리 아이템을 드랍존에 놓으면 플레이어 앞 바닥에 드랍 아이템을 생성한다.
    /// 체스트가 열려 있을 때는 사용하지 않는다.
    /// </summary>
    private void HandleInventoryDropRequested(int slotIndex)
    {
        if (isChestOpen)
            return;

        InventorySlotData slot = playerInventory.GetSlot(slotIndex);
        if (slot == null || playerInventory.IsSlotEmpty(slotIndex))
            return;

        ItemData itemData = playerInventory.GetSlotItemData(slotIndex);
        if (itemData == null)
            return;

        if (itemData.dropPrefab == null)
        {
            Debug.LogWarning($"드랍 프리팹이 연결되지 않았어. TID: {itemData.TID}");
            return;
        }

        int dropCount = slot.amount;

        Vector3 dropPosition = transform.position
            + transform.forward * dropForwardDistance
            + Vector3.up * dropUpOffset;

        // 드랍 프리팹이 GameObject가 아닌 다른 Unity 오브젝트로 잡혀 있어도
        // 런타임에서 최대한 안전하게 실제 게임오브젝트를 찾아 생성한다.
        UnityEngine.Object prefabObject = itemData.dropPrefab;

        Debug.Log($"인벤토리 드랍 생성 시도: item={itemData.itemName}, prefab={prefabObject.name}, type={prefabObject.GetType().Name}");
        UnityEngine.Object spawnedObject = Instantiate(prefabObject, dropPosition, Quaternion.identity);

        GameObject dropObject = null;

        if (spawnedObject is GameObject prefabGameObject)
            dropObject = prefabGameObject;
        else if (spawnedObject is Component prefabComponent)
            dropObject = prefabComponent.gameObject;

        if (dropObject == null)
        {
            Debug.LogError($"드랍 프리팹 생성 결과를 GameObject로 변환하지 못했어. item={itemData.itemName}, spawnedType={spawnedObject.GetType().Name}");
            Destroy(spawnedObject);
            return;
        }

        DropItem dropItem = dropObject.GetComponent<DropItem>();

        if (dropItem == null)
        {
            Debug.LogError("드랍 프리팹에 DropItem 컴포넌트가 없어.");
            Destroy(dropObject);
            return;
        }

        dropItem.itemData = itemData;
        dropItem.stackCount = dropCount;

        // 월드 드랍 생성이 끝난 뒤 인벤토리에서 수량을 제거한다.
        playerInventory.RemoveAmount(slotIndex, dropCount);
        DataManager.Instance.SaveGame();
    }

    private void OpenResultUI(bool result)
    {
        Debug.Log("결과 창을 출력합니다.");
        resultUI = UIManager.Instance.Open<ResultUI>();

        if (resultUI == null)
            return;

        resultUI.UpdateResultUI(result);
    }

    public int FindItemCount(int tid)
    {
        foreach (InventorySlotData slot in playerInventory.slots)
        {
            if (slot.TID == tid)
                return slot.amount;
        }

        return 0;
    }
}
