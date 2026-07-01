/// <summary>
/// 아이템 슬롯 하나의 역할을 수행합니다
/// 인벤토리 슬롯 1칸의 표시와 입력을 담당한다.
/// 인벤토리 슬롯끼리의 위치 교환, 체스트와의 우클릭 이동, 드래그 앤 드롭 이동을 처리한다.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("슬롯 UI 요소")]
    [SerializeField] private Image itemImg;
    [SerializeField] private TMP_Text itemStack;
    [SerializeField] private Transform itemInfo;

    private InventoryUI inventoryUI;

    public int slotIndex { get; set; }

    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;

    private void Awake()
    {

        inventoryUI = GetComponentInParent<InventoryUI>();
        mainCanvas = GetComponentInParent<Canvas>();

        // 프리팹에 수동 연결이 빠졌을 때를 대비해 같은 이름의 자식을 자동으로 찾는다.
        if (itemInfo == null)
            itemInfo = transform.Find("ItemInfo");

        if (itemInfo != null)
            canvasGroup = itemInfo.GetComponent<CanvasGroup>();

        if (mainCanvas == null)
        {
            enabled = false;
            Debug.LogError("InventorySlotUI: Canvas를 찾을 수 없습니다.");
        }
    }

    public void Initialize(int index)
    {
        slotIndex = index;
        UpdateSlot(0, null);
    }

    public void UpdateSlot(int stack, Sprite sprite)
    {
        if (stack <= 0 || sprite == null)
        {
            itemImg.enabled = false;
            itemStack.text = "";
            return;
        }

        itemImg.enabled = true;
        itemImg.sprite = sprite;
        itemStack.text = stack.ToString();
    }

    // 왼쪽 버튼 더블클릭으로 인벤토리 이동을 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // 왼쪽 버튼 클릭이 아니면 처리하지 않기
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // 1번만 클릭한 경우에는 처리하지 않기
        if (eventData.clickCount < 2)
        {
            return;
        }

        if (!itemImg.enabled)
        {
            return;
        }

        // 체스트가 열려 있을 때만 인벤토리 -> 체스트 이동을 허용한다.
        if (ChestUI.ActiveUI != null)
            ChestUI.ActiveUI.TryMoveFromInventory(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mainCanvas == null || itemInfo == null || !itemImg.enabled)
            return;

        itemInfo.SetParent(mainCanvas.transform);
        itemInfo.SetAsLastSibling();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
        inventoryUI?.ShowDropZone();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemInfo == null)
            return;

        itemInfo.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (mainCanvas != null && itemInfo != null && itemInfo.parent == mainCanvas.transform)
        {
            itemInfo.SetParent(transform);
            itemInfo.localPosition = Vector3.zero;
        }
        inventoryUI?.HideDropZone();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null)
            return;

        if (droppedObj.TryGetComponent<InventorySlotUI>(out var originSlot))
        {
            if (originSlot == this)
                return;

            GlobalEventBus.OnSwapInventorySlot?.Invoke(slotIndex, originSlot.slotIndex);
            return;
        }

        if (droppedObj.TryGetComponent<ChestSlotUI>(out var chestSlot))
        {
            if (chestSlot.OwnerUI == null)
                return;

            chestSlot.OwnerUI.TryMoveToInventorySlot(chestSlot.SlotIndex, slotIndex);
        }
    }
}
