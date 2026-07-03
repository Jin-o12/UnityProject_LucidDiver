/// <summary>
/// 아이템과 인벤토리에 관한 상호작용을 중재하는 Presenter.
/// 아이템 습득, 인벤토리 UI 열기/닫기, 체스트 UI 연결, 월드 드랍을 담당한다.
/// </summary>
using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    // 게임 데이터 스크립트 참조
    private PlayerWeapon playerWeapon;                  // 플레이어 무기
    private PlayerInventory playerInventory;            // 플레이어 인벤토리
    private LocalInputReader localInputReader;          // 플레이어 입력 처리
    private EntityIdentity identity;                    // 플레이어 고유 ID

    // 컨테이너 상태 캐시
    private ItemBox currentBox;                         // 현재 열려 있는 상자
    private bool isChestOpen;                           // 상자 UI가 열려 있는지 여부
    private const float dropForwardDistance = 1.0f;     // 아이템을 플레이어 앞에 떨어뜨릴 거리
    private const float dropUpOffset = 0.3f;            // 바닥 겹침 방지용 높이 보정
    private PlayerStatus playerStatus;                  // 플레이어 상태

    // UI 캐시
    private InventoryUI inventoryUI;                    // 인벤토리 UI 캐시
    private ResultUI resultUI;                          // 결과 UI 캐시
    private ChestUI chestUI;                            // 상자 UI 캐시

    // 저장 데이터 인터페이스
    private IItemDataRepository itemRepo;               // 아이템 데이터 접근 인터페이스

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerInventory = GetComponent<PlayerInventory>();
        localInputReader = GetComponent<LocalInputReader>();
        identity = GetComponent<EntityIdentity>();
        playerStatus = GetComponent<PlayerStatus>();

        if (playerWeapon == null || playerInventory == null || playerStatus == null || localInputReader == null || identity == null)
        {
            enabled = false;
            Debug.LogError("InventoryPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }
        
        // 인터페이스 구현부 연결
        itemRepo = new SOItemRepository();

        // 인벤토리 슬롯 초기화
        PlayerSaveData playerData = DataManager.Instance.playerData;
        playerInventory.Initialize(playerData.invenSlotNum, playerData.quickSlotsNum);
    }

    private void OnEnable()
    {
        // 바닥 아이템을 주웠을 때 발생하는 이벤트를 구독한다.
        GlobalEventBus.OnItemPickedUp += HandleItemPickUp;

        // 상자와 상호작용했을 때 상자 UI를 여는 이벤트를 구독한다.
        GlobalEventBus.OnItemBoxOpened += HandleItemBoxOpened;

        // 입력으로 인벤토리 열기/닫기 요청이 들어오면 Presenter가 처리한다.
        localInputReader.OnInventoryOpenRequested += OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested += CloseInventoryUI;

        // 인벤토리 슬롯 데이터가 바뀌면 해당 슬롯 UI를 갱신한다.
        playerInventory.OnSlotChanged += HandleSlotChanged;

        // 인벤토리 드랍존으로 버리기 요청이 들어오면 월드 드랍으로 처리한다.
        GlobalEventBus.OnInventoryDropRequested += HandleInventoryDropRequested;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        GlobalEventBus.OnItemPickedUp -= HandleItemPickUp;
        GlobalEventBus.OnItemBoxOpened -= HandleItemBoxOpened;

        localInputReader.OnInventoryOpenRequested -= OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested -= CloseInventoryUI;

        playerInventory.OnSlotChanged -= HandleSlotChanged;

        GlobalEventBus.OnInventoryDropRequested -= HandleInventoryDropRequested;
    }

    /// <summary>
    /// 인벤토리 UI에서 추후 직접 장착 버튼을 연결할 때를 위한 확장 지점.
    /// </summary>
    // public void OnRequestEquipWeapon(string weaponTID)
    // {}

    /// <summary>
    /// 기존 이벤트 기반 아이템 습득 진입점.
    /// 현재 플레이어가 실제 수령한 수량만 처리하도록 TryPickUpItem으로 위임한다.
    /// </summary>
    private void HandleItemPickUp(int pickerID, int pickedItemTID, int count)
    {
        if (identity.entityID != pickerID)
            return;

        ItemData data = itemRepo.GetItemData(pickedItemTID);
        if (data == null)
            return;

        TryPickUpItem(data, count);
    }

    /// <summary>
    /// 아이템을 실제로 얼마나 주울 수 있었는지 계산하고,
    /// 받지 못한 남은 수량을 반환한다.
    /// </summary>
    public int TryPickUpItem(ItemData data, int count)
    {
        if (data == null || count <= 0)
            return count;

        int remain = count;
        int pickedItemTID = data.TID;

        // TID 100대는 무기
        if (100 < pickedItemTID && pickedItemTID < 200)
        {
            // 무기가 비어 있으면 첫 1개는 바로 장착한다.
            if (!playerWeapon.isEquipped)
            {
                WeaponItemData weaponData = data as WeaponItemData;
                if (weaponData != null)
                {
                    playerWeapon.EquipWeapon(weaponData);
                    remain = Mathf.Max(0, remain - 1);
                }
                else
                {
                    Debug.LogError("무기가 아닌 아이템이 무기 TID를 가지고 있습니다.");
                    return count;
                }
            }

            // 남은 수량이 있으면 인벤토리에 수납한다.
            if (remain > 0)
                remain = playerInventory.AddItem(data, remain);
        }
        // TID 300대는 소모품
        else if (300 < pickedItemTID && pickedItemTID < 400)
        {
            remain = playerInventory.AddItem(data, remain);
        }
        // TID 400대는 특수 아이템
        else if (400 < pickedItemTID && pickedItemTID < 500)
        {
            remain = playerInventory.AddItem(data, remain);
        }
        else
        {
            Debug.LogWarning("Unknown item TID: " + pickedItemTID);
            return count;
        }

        // 아이템 획득 직후에는 저장하지 않는다
        // 획득 아이템은 GameScene의 PlayerInventory에만 보관하고, 결과 정산 시 ResultManager가 저장한다
        // if (remain < count)
        //     DataManager.Instance.SaveGame();

        return remain;
    }

    /// <summary>
    /// 상자와 상호작용했을 때 상자 UI와 인벤토리 UI를 함께 연다.
    /// </summary>
    private void HandleItemBoxOpened(IInteractable interactable, int playerID)
    {
        // 다른 플레이어가 연 상자면 무시한다.
        if (identity.entityID != playerID)
            return;

        // 대상이 ItemBox가 아니면 무시한다.
        ItemBox box = interactable as ItemBox;
        if (box == null)
            return;

        // 현재 연 상자 상태를 기록한다.
        currentBox = box;
        isChestOpen = true;

        // 상자 UI를 여는 동안 입력 맵을 UI 모드로 전환한다.
        localInputReader.SwitchToUIMap();

        // 인벤토리 UI를 먼저 연다.
        OpenInventoryUI();

        // 상자 UI를 열고 데이터와 바인딩한다.
        chestUI = UIManager.Instance.Open<ChestUI>();
        if (chestUI == null) return;

        chestUI.Bind(box, playerInventory, CloseContainerUI);
    }

    /// <summary>
    /// 특정 인벤토리 슬롯 데이터가 바뀌면 해당 슬롯 UI만 갱신한다.
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
        // 플레이어 상태가 idle이 아니면 인벤토리 조작을 막는다.
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        inventoryUI = UIManager.Instance.Open<InventoryUI>();
        if (inventoryUI == null)
            return;

        localInputReader.SetInventoryOpenState(true);

        // 상자 UI가 열려 있을 때는 드랍존을 비활성화한다.
        inventoryUI.SetDropZoneAvailable(!isChestOpen);

        // 슬롯 개수에 맞춰 UI를 생성한다.
        inventoryUI.CreatSlots(playerInventory.slots.Count);

        // 현재 인벤토리 데이터를 슬롯 UI에 반영한다.
        for (int i = 0; i < playerInventory.slotNum; i++)
            inventoryUI.UpdateSlot(i, playerInventory.slots[i]);

        // 일반 인벤토리는 Player 액션맵을 유지한다.
    }

    /// <summary>
    /// 인벤토리 UI를 닫는다.
    /// 상자 UI가 열려 있으면 컨테이너 UI 전체를 함께 닫는다.
    /// </summary>
    public void CloseInventoryUI()
    {
        // 플레이어 상태가 idle이 아니면 인벤토리 조작을 막는다.
        if (playerStatus.nowState != PlayerStatus.livingState.idle)
            return;

        // 상자 UI가 열려 있으면 컨테이너 UI 전체를 닫는다.
        if (isChestOpen)
        {
            CloseContainerUI();
            return;
        }

        UIManager.Instance.Close<InventoryUI>();
        inventoryUI = null;
        localInputReader.SetInventoryOpenState(false);

        // 일반 인벤토리를 닫으면 플레이어 입력으로 복귀한다.
        localInputReader.SwitchToPlayerMap();
    }

    /// <summary>
    /// 상자 UI와 인벤토리 UI를 함께 닫는다.
    /// </summary>
    public void CloseContainerUI()
    {
        // 현재 열려 있던 상자의 열린 상태를 해제한다.
        if (currentBox != null)
            currentBox.CloseBox();

        UIManager.Instance.Close<ChestUI>();
        UIManager.Instance.Close<InventoryUI>();

        // 캐시와 상태를 초기화한다.
        chestUI = null;
        inventoryUI = null;
        currentBox = null;
        isChestOpen = false;
        localInputReader.SetInventoryOpenState(false);

        // 입력 맵을 다시 플레이어 모드로 전환한다.
        localInputReader.SwitchToPlayerMap();
    }

    private void OpenResultUI(bool _result)
    {
        Debug.Log("결과 창을 출력합니다.");

        resultUI = UIManager.Instance.Open<ResultUI>();

        if (resultUI == null) return;
        resultUI.UpdateResultUI(_result);
    }

    public int FindItemCount(int _tid)
    {
        foreach (InventorySlotData slot in playerInventory.slots)
        {
            if (slot.TID == _tid)
            {
                return slot.amount;
            }
        }
        return 0;
    }

    private void HandleInventoryDropRequested(int slotIndex)
    {
        if (isChestOpen)
            return;

        Vector3 dropPosition = transform.position
            + transform.forward * dropForwardDistance
            + Vector3.up * dropUpOffset;

        //playerInventory.TryDropSlotToWorld(slotIndex, dropPosition, () => DataManager.Instance.SaveGame());
        // {인게임 드롭 후에는 즉시 저장하지 않는다}
        // {최종 저장은 결과 정산 시점에 처리한다}
        playerInventory.TryDropSlotToWorld(slotIndex, dropPosition);
    }
}
