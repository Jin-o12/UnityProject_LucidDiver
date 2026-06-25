using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 체스트 슬롯 1칸의 표시와 입력을 담당한다.
/// 우클릭으로 인벤토리 이동, 드래그앤드롭으로 슬롯 지정 이동을 처리한다.
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

        // 프리팹에 직접 연결이 빠져 있어도 동일한 자식 이름을 찾아 자동 연결한다.
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
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
            itemInfo.SetParent(this.transform);
            itemInfo.localPosition = Vector3.zero;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null)
            return;

        if (droppedObj.TryGetComponent<InventorySlotUI>(out var inventorySlot))
        {
            // 인벤토리 슬롯에서 체스트 특정 칸으로 드롭했을 때 그 칸으로 이동을 시도한다.
            chestUI.TryMoveFromInventorySlot(inventorySlot.slotIndex, slotIndex);
        }
    }
}
