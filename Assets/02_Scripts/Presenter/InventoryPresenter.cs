/// <summary>
/// 아이템과 인벤토리에 관한 모든 상호작용의 중재자 역할을 수행합니다.
/// 아이템 습득 시 상황에 따라 즉시 장착하거나 인벤토리에 수납합니다.
/// </summary>
using System.Collections;
using UnityEditor.Graphs;
using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    // 게임 데이터 스크립트 참조
    private PlayerWeapon playerWeapon;                  // 플레이어 무기
    private PlayerInventory playerInventory;            // 플레이어 인벤토리
    private LocalInputReader localInputReader;          // 플레이어 입력 처리
    private PlayerStatus playerStatus;                  // 플레이어 상태

    // UI 캐시
    private InventoryUI inventoryUI;                    // 인벤토리 UI 캐시
    private ResultUI resultUI;                          // 결과창 UI 캐시

    private void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        playerInventory = GetComponent<PlayerInventory>();
        localInputReader = GetComponent<LocalInputReader>();
        playerStatus = GetComponent<PlayerStatus>();

        if (playerWeapon==null || playerInventory==null || localInputReader==null)
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
        localInputReader.OnInventoryOpenRequested += OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested += CloseInventoryUI;

        // 인벤토리 데이터 변경 이벤트를 구독 합니다
        playerInventory.OnSlotChanged += HandleSlotChanged;

        // 게임 종료시 이벤트
        //GlobalEventBus.OnShowGameResult += OpenResultUI;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 구독 해제
        GlobalEventBus.OnItemPickedUp -= HandleItemPickUp;
        localInputReader.OnInventoryOpenRequested -= OpenInventoryUI;
        localInputReader.OnInventoryCloseRequested -= CloseInventoryUI;

        playerInventory.OnSlotChanged -= HandleSlotChanged;

        //GlobalEventBus.OnShowGameResult -= OpenResultUI;
    }

    /// <summary>
    /// 인벤토리 UI에서 유저가 직접 장착 버튼을 누르는 경우 (인벤토리 구현 후 개발)
    /// </summary>
    // public void OnRequestEquipWeapon(string weaponTID)
    // {}

    /* 아이템 줍기 이벤트 신호를 받았을 때 자동 장착 혹은 인벤토리 수납 수행 */
    private void HandleItemPickUp(int pickerID, int pickedItemTID, int count)
    {
        //Debug.Log("Item picked up with TID: " + pickedItemTID);

        // DataManager를 통해 ID로 아이템 원본 데이터를 찾음
        ItemData data = DataManager.Instance.GetItemData(pickedItemTID);
        if(data==null) return;

        // 주운 아이템을 인벤토리에 먼저 추가 후 하위 작업 진행(이후 구현)
        // TID 100대는 무기
        if(100 < pickedItemTID && pickedItemTID < 200)
        {
            //Debug.Log("Weapon item added to inventory.");
            // 만약 장착하고 있는 무기가 없다면 무기를 장착하고, 그렇지 않다면 인벤토리에 수납
            if(!playerWeapon.isEquipped)
            {
                WeaponItemData weaponData = data as WeaponItemData;
                if(weaponData!=null)
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
        else if(300 < pickedItemTID && pickedItemTID < 400)
        {
            //Debug.Log("Consume item added to inventory.");
            playerInventory.AddItem(data, count);
        }
        // TID 400대는 파밍 아이템
        else if(400 < pickedItemTID && pickedItemTID < 500)
        {
            //Debug.Log("General item added to inventory.");
            int throwItem = playerInventory.AddItem(data, count);

            /// ※ 버려지는 아이템에 대한 로직 추가 ///
        }
        // 그 외 아이템은 알 수 없는 아이템
        else
        {
            Debug.LogWarning("Unknown item TID: " + pickedItemTID);
        }

        // DataManager의 playerData 저장
        DataManager.Instance.SaveGame();
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
        // 플레이어 상태가 idle이 아니면 인벤토리 창 조작을 수행하지 않음
        if (playerStatus.nowState != PlayerStatus.livingState.idle) return;

        // 인벤토리 UI 활성화 및 UI 오브젝트 캐시 저장
        inventoryUI = UIManager.Instance.Open<InventoryUI>();
        if(inventoryUI==null) return;

        // 인벤토리 첫 실행시 슬롯 생성
        inventoryUI.CreatSlots(playerInventory.slotNum);

        // 인벤토리의 아이템 상태를 동기화
        for(int i=0; i<playerInventory.slotNum; i++)
            inventoryUI.UpdateSlot(i, playerInventory.slots[i]);
    }

    /* 인벤토리 UI 비활성화 */
    public void CloseInventoryUI()
    {
        // 플레이어 상태가 idle이 아니면 인벤토리 창 조작을 수행하지 않음
        if (playerStatus.nowState != PlayerStatus.livingState.idle) return;

        UIManager.Instance.Close<InventoryUI>();
    }

    private void OpenResultUI(bool _result)
    {
        Debug.Log("결과 창 패널을 출력합니다...");
        // UIManager에서 Canvas-ResultPanel을 받아와 실행
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
}
