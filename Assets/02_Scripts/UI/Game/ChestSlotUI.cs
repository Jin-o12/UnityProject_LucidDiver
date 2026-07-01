using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 체스트 슬롯 1칸의 표시와 입력을 담당한다.
/// 더블클릭으로 인벤토리 이동, 드래그 앤 드롭으로 슬롯 지정 이동을 처리한다.
/// </summary>
public class ChestSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image itemImg;
    [SerializeField] private TMP_Text itemStack;
    [SerializeField] private Transform itemInfo;

    private ChestUI chestUI;
    private int slotIndex;
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;

    public int SlotIndex => slotIndex;
    public ChestUI OwnerUI => chestUI;

    private void Awake()
    {
        mainCanvas = GetComponentInParent<Canvas>();

        // 프리팹에 수동 연결이 빠졌을 때를 대비해 같은 이름의 자식을 자동으로 찾는다.
        if (itemInfo == null)
            itemInfo = transform.Find("ItemInfo");

        if (itemInfo != null)
            canvasGroup = itemInfo.GetComponent<CanvasGroup>();
    }

    public void Initialize(ChestUI owner, int index)
    {
        chestUI = owner;
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

        // 상자 UI에서 인벤토리로 이동 처리를 실행
        chestUI.TryMoveToInventory(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mainCanvas == null || itemInfo == null || !itemImg.enabled)
            return;

        itemInfo.SetParent(mainCanvas.transform);
        itemInfo.SetAsLastSibling();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
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
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null)
            return;

        if (droppedObj.TryGetComponent<InventorySlotUI>(out var inventorySlot))
            chestUI.TryMoveFromInventorySlot(inventorySlot.slotIndex, slotIndex);
    }
}