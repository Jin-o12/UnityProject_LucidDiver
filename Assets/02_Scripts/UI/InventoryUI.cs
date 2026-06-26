using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 인벤토리 UI 출력만 담당한다.
/// 실제 데이터 저장은 하지 않고 슬롯 생성/갱신과 드랍존 표시만 처리한다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] private Transform slotContainer;            // 인벤토리 슬롯이 생성될 부모
    [SerializeField] private List<GameObject> slotsObj = new();  // 생성된 인벤토리 슬롯 오브젝트 목록
    [SerializeField] private GameObject slotPrefab;              // 인벤토리 슬롯 프리팹

    [Header("Drop Zone UI")]
    [SerializeField] private InventoryDropZoneUI dropZone;       // 드래그 중 버리기용 드랍존

    private CanvasGroup dropZoneCanvasGroup;                     // 드랍존 표시/입력 제어용
    private bool canUseDropZone = true;                          // 현재 드랍존 사용 가능 여부

    private void Awake()
    {
        if (dropZone == null)
            dropZone = GetComponentInChildren<InventoryDropZoneUI>(true);

        if (dropZone != null)
            dropZoneCanvasGroup = dropZone.GetComponent<CanvasGroup>();

        HideDropZone();
    }

    /// <summary>
    /// 인벤토리 슬롯을 count개 생성한다.
    /// 슬롯 수가 같으면 다시 만들지 않는다.
    /// </summary>
    public void CreatSlots(int count)
    {
        if (slotsObj.Count == count)
            return;

        for (int i = 0; i < slotsObj.Count; i++)
        {
            if (slotsObj[i] != null)
                Destroy(slotsObj[i]);
        }

        slotsObj.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            if (slotUI != null)
                slotUI.Initialize(i);

            slotsObj.Add(newSlot);
        }
    }

    /// <summary>
    /// 특정 슬롯 UI를 현재 인벤토리 데이터로 갱신한다.
    /// </summary>
    public void UpdateSlot(int slotNum, InventorySlotData slotData)
    {
        InventorySlotUI slotUI = slotsObj[slotNum].GetComponent<InventorySlotUI>();
        slotUI.UpdateSlot(slotData.amount, slotData.icon);
    }

    /// <summary>
    /// 드랍존 사용 가능 여부를 설정한다.
    /// 체스트가 열려 있으면 드랍존을 끈다.
    /// </summary>
    public void SetDropZoneAvailable(bool available)
    {
        canUseDropZone = available;

        if (!canUseDropZone)
            HideDropZone();
    }

    /// <summary>
    /// 드래그 중일 때만 드랍존을 보이게 한다.
    /// </summary>
    public void ShowDropZone()
    {
        if (!canUseDropZone || dropZoneCanvasGroup == null)
            return;

        dropZoneCanvasGroup.alpha = 1f;
        dropZoneCanvasGroup.blocksRaycasts = true;
        dropZoneCanvasGroup.interactable = true;
    }

    /// <summary>
    /// 드래그가 끝나면 드랍존을 숨긴다.
    /// </summary>
    public void HideDropZone()
    {
        if (dropZoneCanvasGroup == null)
            return;

        dropZoneCanvasGroup.alpha = 0f;
        dropZoneCanvasGroup.blocksRaycasts = false;
        dropZoneCanvasGroup.interactable = false;
    }
}
