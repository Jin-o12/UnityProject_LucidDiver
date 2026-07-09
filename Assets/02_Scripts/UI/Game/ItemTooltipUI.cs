using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    [Header("UI 배치")]
    [SerializeField] private RectTransform tooltipPanel; // 툴팁 창 위치
    [SerializeField] private TMP_Text textName;         // 아이템 이름 표시
    [SerializeField] private TMP_Text textType;         // 등급 / 종류 표시
    [SerializeField] private TMP_Text textEffect;       // 효과 수치 및 설명 출력 공간
    [SerializeField] private TMP_Text textFlavor;       // 플레이버 텍스트 출력 공간
    [SerializeField] private TMP_Text textPrice;        // 판매 가치 표시
    [SerializeField] private TMP_Text textMultiple;     // 중첩 한도 표시

    // 출력할 데이터
    public int currentItemTID;                  //선택한 슬롯에 장착된 아이템의 TID
    private string currentItemName;             //아이템 이름
    private ItemGrade currentItemGrade;         //아이템 등급
    private itemCategory currentItemCategory;   //아이템 종류
    private string currentItemMainEffect;       //아이템 효과 텍스트
    private string currentItemDesc;             //아이템 설명 텍스트
    private string currentItemFlavor;           //아이템 플레이버 텍스트
    private int currentItemPrice;               //아이템 판매가
    private int currentItemMultiple;            //아이템 최대 중첩 개수

    private LocalJsonItemRepository itemRepo;   //아이템 데이터 리포지토리

    // 툴팁 창을 출력할 위치 좌표
    public bool isFromInventory = true;                                         //인벤토리 슬롯에 호버 중인지 체크 (false = 상자 슬롯에 호버)
    private Vector2 panelPositionFromInventory = new Vector2(1100, 400);        //인벤토리 슬롯에 호버 시 출력하는 위치
    private Vector2 panelPositionFromChest = new Vector2(700, 400);             //상자 슬롯에 호버 시 출력하는 위치

    // 아이템 이름 색상
    private Color normalColor   = new(1.00f, 1.00f, 1.00f);
    private Color uncommonColor = new(0.12f, 1.00f, 0.00f);
    private Color rareColor     = new(0.00f, 0.44f, 0.88f);
    private Color epicColor     = new(0.64f, 0.21f, 0.93f);
    private Color legendColor   = new(1.00f, 0.50f, 0.00f);

    private void Awake()
    {
        itemRepo = new LocalJsonItemRepository();
        RefreshTooltip();
    }

    // 툴팁에 출력할 데이터를 입력
    public void RefreshData(bool hoverFrom, ItemData item)
    {
        // 포인터를 호버한 슬롯이 인벤토리인지 체크
        isFromInventory = hoverFrom;

        if (item != null)
        {
            // 아이템 데이터를 받아와 데이터를 갱신
            currentItemName = item.itemName;
            currentItemGrade = item.itemGrade;
            currentItemCategory = item.category;
            currentItemDesc = item.desc;
            currentItemMultiple = item.itemMultiple;

            // 아이템 카테고리에 따라 효과 텍스트를 출력하는 메소드
            RefreshItemEffect(item);

            // ItemData에 판매가 값이 정해질 때까지 임시로 값을 지정해 출력
            currentItemPrice = item.itemGrade switch
            {
                ItemGrade.normal => 500,
                ItemGrade.uncommon => 1200,
                ItemGrade.rare => 3000,
                ItemGrade.epic => 6000,
                ItemGrade.legend => 12000,
                _ => 0
            };
        }
        // 툴팁 출력을 갱신
        RefreshTooltip();
    }

    // 아이템 카테고리별 세부 데이터를 불러와 효과 텍스트를 출력
    private void RefreshItemEffect(ItemData item)
    {
        switch (item.category)
        {
            case itemCategory.artifact:
                {
                    ArtifactItemData artifact = (ArtifactItemData)item;
                    // 효과 리스트를 출력하기 전 텍스트 초기화
                    currentItemMainEffect = "";
                    currentItemFlavor = artifact.itemFlavorText;
                    break;
                }
            case itemCategory.consume:
                {
                    ConsumeItemData consume = (ConsumeItemData)item;
                    // 효과 리스트를 출력하기 전 텍스트 초기화
                    currentItemMainEffect = "";
                    string addMainEffect = null;
                    foreach (ItemEffect _effect in consume.useEffect)
                    {
                        switch (_effect.effectType)
                        {
                            case EffectType.healHP:
                                {
                                    addMainEffect = $"{_effect.effectTarget}의 현재 HP를 {_effect.effectValue} 회복\n";
                                    break;
                                }
                            case EffectType.healMP:
                                {
                                    addMainEffect = $"{_effect.effectTarget}의 현재 MP를 {_effect.effectValue} 회복\n";
                                    break;
                                }
                        }
                        currentItemMainEffect += addMainEffect;
                    }
                    currentItemFlavor = " ";
                    break;
                }
            case itemCategory.memory:
                {
                    MemoryPieceitemData piece = (MemoryPieceitemData)item;
                    currentItemMainEffect = $"{piece.userType.ToString()}의 동조율 +{piece.linkRateGain}";
                    currentItemFlavor = " ";
                    break;
                }
            default:
                {
                    currentItemMainEffect = " ";
                    currentItemFlavor = " ";
                    break;
                }
        }
    }

    public void RefreshTooltip()
    {
        // 포인터를 호버한 슬롯에 따라 출력 위치를 변경
        if (tooltipPanel != null)
            tooltipPanel.anchoredPosition = isFromInventory ? panelPositionFromInventory : panelPositionFromChest;

        // 아이템 이름 텍스트와 등급별 색상을 출력
        textName.text = currentItemName;
        textName.color = currentItemGrade switch
        {
            ItemGrade.normal    => normalColor,
            ItemGrade.uncommon  => uncommonColor,
            ItemGrade.rare      => rareColor,
            ItemGrade.epic      => epicColor,
            ItemGrade.legend    => legendColor,
            _                   => Color.white
        };

        //아이템 등급과 종류를 출력
        textType.text = $"[ {currentItemGrade.ToString()} - {currentItemCategory.ToString()}]";

        //아이템 효과와 플레이버 텍스트를 출력
        textEffect.text = $"<color=#64F064>{currentItemMainEffect}</color>\n{currentItemDesc}";
        textFlavor.text = currentItemFlavor;

        //아이템 판매가와 최대 중첩 개수를 출력
        textPrice.text = $"판매가: {currentItemPrice}";
        textMultiple.text = $"최대 중첩: {currentItemMultiple}개";
    }
}
