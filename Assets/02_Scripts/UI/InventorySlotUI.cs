using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 1칸의 표시와 입력을 담당한다.
/// 인벤토리 슬롯끼리 교환하고, 체스트가 열려 있으면 체스트와도 아이템을 주고받는다.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("슬롯 UI 요소")]
    [SerializeField] private Image itemImg;
    [SerializeField] private TMP_Text itemStack;
    [SerializeField] private Transform itemInfo;

    public int slotIndex { get; set; }

    private CanvasGroup canvasGroup;
    private InventoryUI inventoryUI;
    private Canvas mainCanvas;

    public event Action<int, int> OnSlotDrop;

    private void Awake()
    {
        mainCanvas = GetComponentInParent<Canvas>();
        inventoryUI = GetComponentInParent<InventoryUI>();

        if (mainCanvas == null || inventoryUI == null)
        {
            this.enabled = false;
            Debug.LogError("InventorySlotUI: 필요한 컴포넌트가 없습니다.");
            return;
        }

        if (itemInfo != null)
            canvasGroup = itemInfo.GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 슬롯을 빈 상태로 초기화하고 인덱스를 기록한다.
    /// </summary>
    public void Initialize(int index)
    {
        itemImg.enabled = false;
        itemStack.text = "";
        slotIndex = index;
    }

    /// <summary>
    /// 전달받은 수량과 아이콘으로 슬롯 UI를 갱신한다.
    /// </summary>
    public void UpdateSlot(int stack, Sprite sprite)
    {
        if (stack <= 0 || sprite == null)
        {
            itemImg.enabled = false;
            itemStack.text = "";
            return;
        }

        itemStack.text = stack.ToString();
        itemImg.sprite = sprite;
        itemImg.enabled = true;
    }

    /// <summary>
    /// 체스트가 열려 있을 때 우클릭하면 인벤토리 아이템을 상자로 옮긴다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (!itemImg.enabled || ChestUI.ActiveUI == null)
            return;

        ChestUI.ActiveUI.TryMoveFromInventory(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemInfo == null || !itemImg.enabled)
            return;

        // 드래그 중 아이콘이 다른 UI 아래로 가려지지 않게 최상단으로 올린다.
        itemInfo.SetParent(mainCanvas.transform);
        itemInfo.SetAsLastSibling();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        inventoryUI.ShowDropZone();
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

        // 드래그 처리에 성공하지 못했으면 원래 슬롯 위치로 돌려놓는다.
        if (itemInfo != null && itemInfo.parent == mainCanvas.transform)
        {
            itemInfo.SetParent(transform);
            itemInfo.localPosition = Vector3.zero;
        }

        inventoryUI.HideDropZone();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null)
            return;

        if (droppedObj.TryGetComponent(out InventorySlotUI originSlot))
        {
            if (originSlot == this)
                return;

            // 인벤토리 슬롯끼리는 기존처럼 서로 위치를 교환한다.
            GlobalEventBus.OnSwapInventorySlot?.Invoke(slotIndex, originSlot.slotIndex);
            OnSlotDrop?.Invoke(slotIndex, originSlot.slotIndex);
            return;
        }

        if (droppedObj.TryGetComponent(out ChestSlotUI chestSlot))
        {
            // 체스트 슬롯에서 인벤토리 특정 칸으로 드랍되면 그 칸으로 이동을 시도한다.
            chestSlot.OwnerUI?.TryMoveToInventorySlot(chestSlot.SlotIndex, slotIndex);
        }
    }
}
