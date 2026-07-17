/// <summary>
/// 아이템 슬롯 하나의 역할을 수행합니다
/// 인벤토리 슬롯 1칸의 표시와 입력을 담당한다.
/// 인벤토리 슬롯끼리의 위치 교환, 체스트와의 우클릭 이동, 드래그 앤 드롭 이동, 포인터 호버로 툴팁 출력을 처리한다.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("슬롯 UI 요소")]
    [SerializeField] private Image slotFrameImage; // 기본 빈 슬롯 이미지
    [SerializeField] private Image rarityFrameImage; // 등급별 슬롯 이미지
    [SerializeField] private Image itemImg;
    [SerializeField] private TMP_Text itemStack;
    [SerializeField] private Transform itemInfo;
    [SerializeField] private SlotType slotType;     //슬롯 종류

    [Header("등급별 슬롯 이미지")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite normalSlotSprite;
    [SerializeField] private Sprite uncommonSlotSprite;
    [SerializeField] private Sprite rareSlotSprite;
    [SerializeField] private Sprite epicSlotSprite;
    [SerializeField] private Sprite legendSlotSprite;

    private InventoryUI inventoryUI;

    public int slotIndex { get; set; }

    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;
    
    // 유니티 좀비 드래그(마우스를 떼지 않고 UI 재활성화 시 OnDrag가 이어지는 현상) 방지용 플래그
    private bool isDragging = false;

    private void Awake()
    {

        inventoryUI = GetComponentInParent<InventoryUI>();
        mainCanvas = GetComponentInParent<Canvas>();

        // 프리팹에 수동 연결이 빠졌을 때를 대비해 같은 이름의 자식을 자동으로 찾는다.
        if (slotFrameImage == null)
            slotFrameImage = GetComponent<Image>();

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

    /* 외부(InventoryUI)에서 인벤토리가 닫히기 직전에 호출하여 비활성화 에러 피하면서 상호작용 초기화 */
    public void ResetDragState()
    {
        isDragging = false; // 드래그 강제 취소 인식

        if (mainCanvas != null && itemInfo != null && itemInfo.parent == mainCanvas.transform)
        {
            itemInfo.SetParent(transform, false);
            itemInfo.localPosition = Vector3.zero;
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
    }

    public void Initialize(int index)
    {
        slotIndex = index;
        UpdateSlot(0, null, ItemGrade.empty, SlotType.empty);
    }

    public void UpdateSlot(int stack, Sprite sprite, SlotType _type = SlotType.inventory)
    {
        UpdateSlot(stack, sprite, ItemGrade.empty, _type);
    }

    public void UpdateSlot(int stack, Sprite sprite, ItemGrade grade, SlotType _type)
    {
        if (stack <= 0 || sprite == null)
        {
            itemImg.enabled = false;
            itemStack.text = "";
            slotType = SlotType.empty;
            ApplySlotFrame(ItemGrade.empty);
            return;
        }

        itemImg.enabled = true;
        itemImg.sprite = sprite;
        itemStack.text = stack.ToString();
        slotType = _type;
        ApplySlotFrame(grade);
    }

    /// <summary>
    /// 엑셀/JSON에서 넘어온 아이템 등급에 맞춰 슬롯 배경 이미지를 교체한다.
    /// </summary>
    /// 빈 슬롯
    ///-> slotFrameImage = emptySlotSprite
    ///-> rarityFrameImage 숨김
    ///-> itemImg 숨김

    ///아이템 있음
    ///-> slotFrameImage = emptySlotSprite 그대로 유지
    ///-> rarityFrameImage = 등급별 Sprite
    ///-> rarityFrameImage 표시
    ///-> itemImg 표시
    private void ApplySlotFrame(ItemGrade grade)
    {
        if (slotFrameImage != null)
        {
            slotFrameImage.sprite = emptySlotSprite;
        }

        if (rarityFrameImage == null)
            return;

        if (grade == ItemGrade.empty)
        {
            rarityFrameImage.enabled = false;
            rarityFrameImage.sprite = null;
            return;
        }

        rarityFrameImage.enabled = true;
        rarityFrameImage.color = Color.white;
        rarityFrameImage.sprite = GetSlotFrameSprite(grade);
    }

    private Sprite GetSlotFrameSprite(ItemGrade grade)
    {
        return grade switch
        {
            ItemGrade.normal => normalSlotSprite != null ? normalSlotSprite : emptySlotSprite,
            ItemGrade.uncommon => uncommonSlotSprite != null ? uncommonSlotSprite : normalSlotSprite,
            ItemGrade.rare => rareSlotSprite != null ? rareSlotSprite : normalSlotSprite,
            ItemGrade.epic => epicSlotSprite != null ? epicSlotSprite : rareSlotSprite,
            ItemGrade.legend => legendSlotSprite != null ? legendSlotSprite : epicSlotSprite,
            _ => emptySlotSprite
        };
    }

    // 왼쪽 버튼 더블클릭으로 인벤토리 이동을 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // 왼쪽 버튼 클릭이 아니면 처리하지 않기
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // 클릭 시 사운드 재생
        GlobalEventBus.OnClickAudio?.Invoke(true);

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

        isDragging = true;
        
        itemInfo.SetParent(mainCanvas.transform);
        itemInfo.SetAsLastSibling();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
        inventoryUI?.ShowDropZone();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // OnBeginDrag를 정상적으로 거치지 않은 '좀비 드래그'는 철저히 무시
        if (!isDragging || itemInfo == null)
            return;

        itemInfo.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

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

        if (droppedObj.TryGetComponent<ArtifactEquipSlotUI>(out var artifactSlot))
        {
            // 장착 슬롯에서 일반 인벤토리 슬롯으로 드롭하면 해제를 요청합니다.
            // 실제로 인벤토리에 들어갈 공간이 있는지는 InventoryPresenter가 판단합니다.
            GlobalEventBus.OnArtifactUnequipRequested?.Invoke(artifactSlot.EquipSlotIndex);
            return;
        }

        if (droppedObj.TryGetComponent<ChestSlotUI>(out var chestSlot))
        {
            if (chestSlot.OwnerUI == null)
                return;

            chestSlot.OwnerUI.TryMoveToInventorySlot(chestSlot.SlotIndex, slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //포인터가 슬롯 UI에 들어오면 아이템 데이터를 읽는다
        GlobalEventBus.OnTooltipUIOpen?.Invoke(slotType, slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //포인터가 슬롯 UI에서 빠져나가면 슬롯 UI를 닫는다
        GlobalEventBus.OnTooltipUIClose?.Invoke();
    }
}
