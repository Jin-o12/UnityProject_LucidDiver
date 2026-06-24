/// <summary>
/// 아이템과 인벤토리에 관한 모든 상호작용의 중재자 역할을 수행합니다.
/// 아이템 습득 시 상황에 따라 즉시 장착하거나 인벤토리에 수납합니다.
/// 인벤토리 UI와 상자 UI의 열기/닫기 흐름도 함께 관리합니다.
/// </summary>
using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    // 게임 데이터 스크립트 참조
    private PlayerWeapon playerWeapon;                  // 플레이어 무기
    private PlayerInventory playerInventory;            // 플레이어 인벤토리
    private LocalInputReader localInputReader;          // 플레이어 입력 처리
    private EntityIdentity identity;                    // 플레이어 고유 ID 참조

    // UI 캐시
    private InventoryUI inventoryUI;                    // 인벤토리 UI 캐시
    private ChestUI chestUI;                            // 상자 UI 캐시

    // 컨테이너 상태 캐시
    private ItemBox currentBox;                         // 현재 열려 있는 상자
    private bool isChestOpen;                           // 상자 UI가 열려 있는지 여부

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerInventory = GetComponent<PlayerInventory>();
        localInputReader = GetComponent<LocalInputReader>();
        identity = GetComponent<EntityIdentity>();

        if (playerWeapon == null || playerInventory == null || localInputReader == null || identity == null)
        {
            this.enabled = false;
            Debug.LogError("InventoryPresenter: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        // 바닥에서 아이템이 주어졌을 때 터지는 전역 이벤트를 구독합니다
        GlobalEventBus.OnItemPickedUp += HandleItemPickUp;

        // 상자와 상호작용했을 때 열림 요청 이벤트를 구독합니다
        GlobalEventBus.OnItemBoxOpened += HandleItemBoxOpened;

        // 입력으로 인벤토리 열기 / 닫기 요청이 들어왔을 때 실행할 메서드를 구독합니다
        localInputReader.OnInventoryOpenRequested += OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested += CloseInventoryUI;

        // 인벤토리 데이터 변경 이벤트를 구독 합니다
        playerInventory.OnSlotChanged += HandleSlotChanged;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        GlobalEventBus.OnItemPickedUp -= HandleItemPickUp;
        GlobalEventBus.OnItemBoxOpened -= HandleItemBoxOpened;

        localInputReader.OnInventoryOpenRequested -= OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested -= CloseInventoryUI;

        playerInventory.OnSlotChanged -= HandleSlotChanged;
    }

    /// <summary>
    /// 인벤토리 UI에서 유저가 직접 장착 버튼을 누르는 경우 (인벤토리 구현 후 개발)
    /// </summary>
    // public void OnRequestEquipWeapon(string weaponTID)
    // {}

    /* 아이템 줍기 이벤트 신호를 받았을 때 자동 장착 혹은 인벤토리 수납 수행 */
    private void HandleItemPickUp(int pickerID, int pickedItemTID, int count)
    {
        // DataManager를 통해 ID로 아이템 원본 데이터를 찾음
        ItemData data = DataManager.Instance.GetItemData(pickedItemTID);
        if (data == null) return;

        // 주운 아이템을 인벤토리에 먼저 추가 후 하위 작업 진행(이후 구현)
        // TID 100대는 무기
        if (100 < pickedItemTID && pickedItemTID < 200)
        {
            // 만약 장착하고 있는 무기가 없다면 무기를 장착하고, 그렇지 않다면 인벤토리에 수납
            if (!playerWeapon.isEquipped)
            {
                WeaponItemData weaponData = data as WeaponItemData;
                if (weaponData != null)
                    playerWeapon.EquipWeapon(weaponData);
                else
                    Debug.LogError("무기가 아닌 아이템이 잘못된 TID를 가지고 있습니다.");
            }
            else
            {
                int throwItem = playerInventory.AddItem(data, count);

                /// ※ 버려지는 아이템에 대한 로직 추가 ///
            }
        }
        // TID 300대는 소모품
        else if (300 < pickedItemTID && pickedItemTID < 400)
        {
            playerInventory.AddItem(data, count);
        }
        // TID 400대는 파밍 아이템
        else if (400 < pickedItemTID && pickedItemTID < 500)
        {
            int throwItem = playerInventory.AddItem(data, count);

            /// ※ 버려지는 아이템에 대한 로직 추가 ///
        }
        // 그 외 아이템은 알 수 없는 아이템
        else
        {
            Debug.LogWarning("Unknown item TID: " + pickedItemTID);
        }
    }

    /* 상자와 상호작용 했을 때 상자 UI와 인벤토리 UI를 함께 오픈 */
    private void HandleItemBoxOpened(IInteractable interactable, int playerID)
    {
        // 다른 플레이어가 연 상자라면 무시
        if (identity.entityID != playerID)
            return;

        // 상호작용한 대상이 ItemBox가 아니면 무시
        ItemBox box = interactable as ItemBox;
        if (box == null)
            return;

        // 현재 열린 상자 상태 저장
        currentBox = box;
        isChestOpen = true;

        // 입력 액션맵을 UI 모드로 전환
        localInputReader.SwitchToUIMap();

        // 왼쪽 인벤토리 UI 오픈
        OpenInventoryUI();

        // 오른쪽 상자 UI 오픈 및 데이터 바인딩
        chestUI = UIManager.Instance.Open<ChestUI>();
        if (chestUI == null) return;

        // 상자 UI가 닫힐 때 현재 Presenter의 CloseContainerUI를 호출하도록 연결
        chestUI.Bind(box, playerInventory, CloseContainerUI);
    }

    /* 특정 칸의 인벤토리 슬롯이 바뀌었을 때 해당 칸을 갱신함 */
    private void HandleSlotChanged(int index)
    {
        if (inventoryUI == null || !inventoryUI.gameObject.activeInHierarchy) return;

        inventoryUI.UpdateSlot(index, playerInventory.slots[index]);
    }

    /* 인벤토리 UI 활성화 */
    public void OpenInventoryUI()
    {
        // 인벤토리 UI 활성화 및 UI 오브젝트 캐시 저장
        inventoryUI = UIManager.Instance.Open<InventoryUI>();
        if (inventoryUI == null) return;

        // 인벤토리 첫 실행시 슬롯 생성
        inventoryUI.CreatSlots(playerInventory.slotNum);

        // 인벤토리의 아이템 상태를 동기화
        for (int i = 0; i < playerInventory.slotNum; i++)
            inventoryUI.UpdateSlot(i, playerInventory.slots[i]);
    }

    /* 인벤토리 UI 비활성화 */
    public void CloseInventoryUI()
    {
        // 상자 UI가 열려 있으면 인벤토리만 따로 닫지 않고 컨테이너 UI 전체를 닫음
        if (isChestOpen)
        {
            CloseContainerUI();
            return;
        }

        UIManager.Instance.Close<InventoryUI>();
        inventoryUI = null;

        // 일반 인벤토리를 닫았을 때는 다시 플레이 조작으로 복귀
        localInputReader.SwitchToPlayerMap();
    }

    /* 상자 UI와 인벤토리 UI를 함께 닫음 */
    public void CloseContainerUI()
    {
        // 현재 열려 있던 상자의 열림 상태 해제
        if (currentBox != null)
            currentBox.CloseBox();

        // 상자 UI와 인벤토리 UI를 함께 닫음
        UIManager.Instance.Close<ChestUI>();
        UIManager.Instance.Close<InventoryUI>();

        // 캐시 및 상태값 초기화
        chestUI = null;
        inventoryUI = null;
        currentBox = null;
        isChestOpen = false;

        // 입력 액션맵을 다시 플레이어 모드로 전환
        localInputReader.SwitchToPlayerMap();
    }
}