using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class QuickSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("슬롯 내 요소")]
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

        if(mainCanvas==null || quickSlotGroupUI==null)
        {
            this.enabled = false;
            Debug.LogError("InventorySlotUI: 필요한 컴포넌트가 없습니다.");
            return;
        }

        if(itemInfo!=null)
            canvasGroup = itemInfo.GetComponent<CanvasGroup>();
    }

    public void Initialize(int _index)
    {
        slotIcon.enabled = false;
        itemStack.text = "";
        slotIndex = _index;
    }

    /* 해당 슬롯의 UI를 변경하는 함수 (아이템 갯수, 스프라이트 이미지) */
    public void UpdateSlot(int _stack, Sprite _sprite)
    {
        // 들어있는 아이템이 없을 경우 빈 공간으로 초기화
        if(_stack==0 || _sprite==null)
        {
            slotIcon.enabled = false;
            itemStack.text = "";
            return;
        }
        
        itemStack.text = $"{_stack}";
        slotIcon.sprite = _sprite;
        slotIcon.enabled = true;
    }

    /* 드래그 시작 시 호출 */
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 빈 슬롯이여서 옮길 아이콘이 없다면 드래그 취소
        if(itemInfo==null || !slotIcon.enabled) return;

        // 캔버스 최상단으로 올려 가장 위에 보이게 함
        itemInfo.SetParent(mainCanvas.transform);
        itemInfo.SetAsLastSibling();

        // 아이콘의 마우스 방해 끄기
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    /* 드래그 중 지속적으로 호출 */
    public void OnDrag(PointerEventData eventData)
    {
        itemInfo.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 아이콘의 마우스 방해 다시 켜기
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        // 만약 자식이 다른 슬롯에 드래그되지 못했다면 원위치로 복귀
        if (itemInfo.parent == mainCanvas.transform)
        {
            itemInfo.SetParent(this.transform);
            itemInfo.localPosition = Vector3.zero; // 한가운데로 정렬
        }
    }

    /* 자신의 위에 무언가 드롭 되었을 떄 호출 */
    public void OnDrop(PointerEventData eventData)
    {
        // 직전까지 유저가 드래그하던 오브젝트
        GameObject droppedObj = eventData.pointerDrag;

        if(droppedObj!=null)
        {
            if(droppedObj.TryGetComponent<InventorySlotUI>(out var originSlot))
            {
                // 두 슬롯간에 교환이 있었음을 방송
                GlobalEventBus.OnDropItemQuickSlot(slotIndex, originSlot.slotIndex);
            }
        }
    }
}
