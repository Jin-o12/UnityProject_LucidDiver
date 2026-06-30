using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("슬롯 구성 요소")]
    [SerializeField] public Image slotIcon;
    [SerializeField] public TMP_Text itemStack;
    [SerializeField] private Transform itemInfo;
    public int slotIndex { get; set; }

    private CanvasGroup canvasGroup;
    private QuickSlotGroupUI quickSlotGroupUI;
    private Canvas mainCanvas;

    private void Awake()
    {
        mainCanvas = GetComponentInParent<Canvas>();
        quickSlotGroupUI = GetComponentInParent<QuickSlotGroupUI>();

        if (mainCanvas == null || quickSlotGroupUI == null)
        {
            enabled = false;
            Debug.LogError("QuickSlotUI: 필요한 컴포넌트가 없습니다.");
            return;
        }

        if (itemInfo != null)
        {
            canvasGroup = itemInfo.GetComponent<CanvasGroup>();
        }
    }

    public void Initialize(int _index)
    {
        slotIcon.enabled = false;
        itemStack.text = "";
        slotIndex = _index;
    }

    /* 해당 슬롯 UI를 변경하는 함수 (아이템 개수, 스프라이트 이미지) */
    public void UpdateSlot(int _stack, Sprite _sprite)
    {
        // 등록된 아이템 아이콘 자체가 없을 때만 빈 슬롯처럼 숨깁니다.
        if (_sprite == null)
        {
            slotIcon.enabled = false;
            itemStack.text = "";
            return;
        }

        slotIcon.sprite = _sprite;
        slotIcon.enabled = true;

        // 수량이 0이어도 등록 정보는 유지하므로 그대로 표시합니다.
        itemStack.text = $"{Mathf.Max(0, _stack)}";
    }

    /* 드래그 시작 시 호출 */
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemInfo == null || !slotIcon.enabled) return;

        itemInfo.SetParent(mainCanvas.transform);
        itemInfo.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    /* 드래그 중 지속적으로 호출 */
    public void OnDrag(PointerEventData eventData)
    {
        itemInfo.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (itemInfo.parent == mainCanvas.transform)
        {
            itemInfo.SetParent(transform);
            itemInfo.localPosition = Vector3.zero;
        }
    }

    /* 자신 위에 무언가 드롭되었을 때 호출 */
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;

        if (droppedObj == null)
        {
            return;
        }

        if (droppedObj.TryGetComponent<InventorySlotUI>(out var originSlot))
        {
            // 인벤토리에서 퀵슬롯으로 등록 요청
            GlobalEventBus.OnDropItemQuickSlot(slotIndex, originSlot.slotIndex);
        }
        else if (droppedObj.TryGetComponent<QuickSlotUI>(out var quickSlot))
        {
            // 퀵슬롯끼리 자리 교환
            GlobalEventBus.OnSwapItemQuickSlot(quickSlot.slotIndex, slotIndex);
        }
    }
}
