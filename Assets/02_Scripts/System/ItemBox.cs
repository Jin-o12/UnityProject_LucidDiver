using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 상호작용할 수 있는 컨테이너 박스
/// 상자는 고정된 슬롯 개수를 가지며,
/// 아이템이 빠져도 슬롯 자체는 삭제되지 않고 빈 칸으로 유지된다.
/// </summary>
public class ItemBox : MonoBehaviour, IInteractable
{
    [Header("Container Settings")]
    [SerializeField] private int slotCapacity = 8;                 // 상자가 가질 고정 슬롯 개수
    [SerializeField] private List<BoxItemEntry> items = new();     // 실제 슬롯 데이터 목록

    [Header("Random Loot Settings")]
    [SerializeField] private bool useRandomLoot = true;               // 시작 시 랜덤 아이템 생성 사용 여부
    [SerializeField] private int minCreateCount = 1;                  // 최소 생성 아이템 종류 수
    [SerializeField] private int maxCreateCount = 2;                  // 최대 생성 아이템 종류 수
    [SerializeField] private bool allowDuplicateLoot = false;         // 같은 아이템 중복 생성 허용 여부
    [SerializeField] private List<BoxLootOption> lootOptions = new(); // 랜덤 후보 아이템 목록

    private bool isOpened = false;                                    // 현재 상자가 열려 있는지 여부

    /// <summary>
    /// 체스트 UI가 읽어갈 슬롯 목록
    /// 슬롯 수는 항상 slotCapacity와 동일하게 유지된다.
    /// </summary>
    public IReadOnlyList<BoxItemEntry> Items => items;

    /// <summary>
    /// 현재 상자가 가지는 총 슬롯 개수
    /// </summary>
    public int SlotCount => items.Count;

    private void Awake()
    {
        EnsureSlotCapacity();

        // 랜덤 생성 모드를 사용하고,
        // 아직 상자 안이 완전히 비어 있을 때만 시작 시 한 번 채운다.
        if (useRandomLoot && IsEmpty())
            GenerateRandomItems();
    }

    private void OnValidate()
    {
        EnsureSlotCapacity();
    }

    /// <summary>
    /// 인스펙터 우클릭 메뉴에서 랜덤 아이템을 다시 생성할 때 사용
    /// </summary>
    [ContextMenu("Generate Random Items")]
    private void GenerateRandomItemsFromContextMenu()
    {
        EnsureSlotCapacity();
        GenerateRandomItems();
    }

    /// <summary>
    /// 플레이어가 상자와 상호작용했을 때 호출
    /// 이미 열려 있으면 중복으로 열지 않는다.
    /// </summary>
    public bool Interact(int playerID)
    {
        if (isOpened)
            return false;

        isOpened = true;

        // Presenter가 체스트 UI를 열 수 있도록 이벤트 전달
        GlobalEventBus.OnItemBoxOpened?.Invoke(this, playerID);

        // 상자는 사라지지 않으므로 false 반환
        return false;
    }

    /// <summary>
    /// 지정한 슬롯 인덱스의 데이터를 반환
    /// </summary>
    public BoxItemEntry GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        return items[index];
    }

    /// <summary>
    /// 지정한 슬롯이 비어 있는지 확인
    /// </summary>
    public bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= items.Count)
            return true;

        BoxItemEntry entry = items[index];
        if (entry == null)
            return true;

        return entry.itemData == null || entry.amount <= 0;
    }

    /// <summary>
    /// 슬롯 하나를 완전히 비움
    /// 슬롯 자체는 삭제하지 않는다.
    /// </summary>
    public void ClearSlot(int index)
    {
        if (index < 0 || index >= items.Count)
            return;

        if (items[index] == null)
            items[index] = CreateEmptyEntry();

        items[index].itemData = null;
        items[index].amount = 0;
    }

    /// <summary>
    /// 슬롯 하나에서 수량을 감소시킴
    /// 수량이 0 이하가 되면 빈 슬롯으로 만든다.
    /// </summary>
    public void RemoveAmount(int index, int amount)
    {
        if (index < 0 || index >= items.Count)
            return;

        if (IsSlotEmpty(index))
            return;

        items[index].amount -= amount;

        if (items[index].amount <= 0)
            ClearSlot(index);
    }

    /// <summary>
    /// 상자 전체가 비어 있는지 확인
    /// 슬롯 개수가 0인지가 아니라, 모든 슬롯이 비었는지를 본다.
    /// </summary>
    public bool IsEmpty()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (!IsSlotEmpty(i))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 상자 UI를 닫을 때 다시 열 수 있도록 상태를 해제
    /// </summary>
    public void CloseBox()
    {
        isOpened = false;
    }

    /// <summary>
    /// 아이템을 상자 안에 넣고, 다 못 넣은 수량을 반환
    /// 먼저 같은 아이템 스택을 채우고, 그 다음 빈 칸에 넣는다.
    /// </summary>
    public int TryAddItem(ItemData itemData, int count)
    {
        if (itemData == null || count <= 0)
            return count;

        int remain = count;

        // 1. 먼저 같은 아이템이 있는 슬롯에 누적
        for (int i = 0; i < items.Count; i++)
        {
            if (IsSlotEmpty(i))
                continue;

            if (items[i].itemData.TID != itemData.TID)
                continue;

            remain = TryAddToSlot(i, itemData, remain);

            if (remain <= 0)
                return 0;
        }

        // 2. 남은 수량을 빈 슬롯에 추가
        for (int i = 0; i < items.Count; i++)
        {
            if (!IsSlotEmpty(i))
                continue;

            remain = TryAddToSlot(i, itemData, remain);

            if (remain <= 0)
                return 0;
        }

        return remain;
    }

    /// <summary>
    /// 특정 슬롯에 아이템을 넣고, 다 못 넣은 수량을 반환
    /// </summary>
    public int TryAddToSlot(int slotIndex, ItemData itemData, int count)
    {
        if (slotIndex < 0 || slotIndex >= items.Count)
            return count;

        if (itemData == null || count <= 0)
            return count;

        if (items[slotIndex] == null)
            items[slotIndex] = CreateEmptyEntry();

        BoxItemEntry slot = items[slotIndex];
        int maxStack = Mathf.Max(1, itemData.itemMultiple);

        // 빈 슬롯이면 새로 배치
        if (IsSlotEmpty(slotIndex))
        {
            int addAmount = Mathf.Min(count, maxStack);

            slot.itemData = itemData;
            slot.amount = addAmount;

            return count - addAmount;
        }

        // 같은 아이템이면 스택 누적
        if (slot.itemData.TID == itemData.TID)
        {
            int canAdd = maxStack - slot.amount;

            if (canAdd <= 0)
                return count;

            int addAmount = Mathf.Min(count, canAdd);
            slot.amount += addAmount;

            return count - addAmount;
        }

        // 다른 아이템이 이미 있으면 넣지 못함
        return count;
    }

    /// <summary>
    /// 랜덤 후보 목록에서 아이템을 뽑아 상자 안 슬롯에 채움
    /// 기존 슬롯은 삭제하지 않고 내용만 비운 뒤 다시 채운다.
    /// </summary>
    private void GenerateRandomItems()
    {
        EnsureSlotCapacity();
        ClearAllSlots();

        int createMin = Mathf.Max(1, minCreateCount);
        int createMax = Mathf.Max(createMin, maxCreateCount);

        List<BoxLootOption> pool = new List<BoxLootOption>();

        for (int i = 0; i < lootOptions.Count; i++)
        {
            if (lootOptions[i] == null || lootOptions[i].itemData == null)
                continue;

            pool.Add(lootOptions[i]);
        }

        if (pool.Count == 0)
            return;

        int createCount = Random.Range(createMin, createMax + 1);

        // 중복 불가일 때는 후보 수 / 슬롯 수를 넘지 않도록 제한
        if (!allowDuplicateLoot)
            createCount = Mathf.Min(createCount, pool.Count);

        createCount = Mathf.Min(createCount, items.Count);

        for (int i = 0; i < createCount; i++)
        {
            if (pool.Count == 0)
                break;

            BoxLootOption selectedOption = PickRandomOption(pool);

            if (selectedOption == null || selectedOption.itemData == null)
                continue;

            int minAmount = Mathf.Max(1, selectedOption.minAmount);
            int maxAmount = Mathf.Max(minAmount, selectedOption.maxAmount);
            int amount = Random.Range(minAmount, maxAmount + 1);

            // 상자 안에 실제로 아이템 넣기
            TryAddItem(selectedOption.itemData, amount);

            if (!allowDuplicateLoot)
                pool.Remove(selectedOption);
        }
    }

    /// <summary>
    /// 가중치 기준으로 랜덤 후보 하나를 선택
    /// </summary>
    private BoxLootOption PickRandomOption(List<BoxLootOption> options)
    {
        int totalWeight = 0;

        for (int i = 0; i < options.Count; i++)
        {
            totalWeight += Mathf.Max(1, options[i].weight);
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < options.Count; i++)
        {
            currentWeight += Mathf.Max(1, options[i].weight);

            if (randomValue < currentWeight)
                return options[i];
        }

        return options[options.Count - 1];
    }

    /// <summary>
    /// 상자의 슬롯 개수를 slotCapacity와 맞춤
    /// 빈 슬롯도 실제 엔트리로 유지한다.
    /// </summary>
    private void EnsureSlotCapacity()
    {
        if (slotCapacity < 1)
            slotCapacity = 1;

        if (items == null)
            items = new List<BoxItemEntry>();

        while (items.Count < slotCapacity)
        {
            items.Add(CreateEmptyEntry());
        }

        while (items.Count > slotCapacity)
        {
            items.RemoveAt(items.Count - 1);
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                items[i] = CreateEmptyEntry();
        }
    }

    /// <summary>
    /// 상자 안의 모든 슬롯을 빈 칸으로 초기화
    /// 슬롯 자체는 삭제하지 않는다.
    /// </summary>
    private void ClearAllSlots()
    {
        for (int i = 0; i < items.Count; i++)
        {
            ClearSlot(i);
        }
    }

    /// <summary>
    /// 빈 슬롯 엔트리를 생성
    /// </summary>
    private BoxItemEntry CreateEmptyEntry()
    {
        return new BoxItemEntry
        {
            itemData = null,
            amount = 0
        };
    }
}