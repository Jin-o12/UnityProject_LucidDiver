using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 상호작용할 수 있는 아이템 상자 클래스입니다.
/// 내부 아이템은 고정 슬롯 방식으로 관리하며,
/// 체스트 UI와 연결될 때 슬롯 인덱스가 유지되도록 설계합니다.
/// </summary>
public class ItemBox : MonoBehaviour, IInteractable
{
    [Header("Container Settings")]
    [SerializeField] private int slotCapacity = 8;                 // 상자가 가질 수 있는 최대 슬롯 수
    [SerializeField] private List<BoxItemEntry> items = new();     // 실제 슬롯 데이터 목록

    [Header("Random Loot Settings")]
    [SerializeField] private bool useRandomLoot = true;               // 시작 시 랜덤 아이템을 생성할지 여부
    [SerializeField] private int minCreateCount = 1;                  // 최소 생성 아이템 종류 수
    [SerializeField] private int maxCreateCount = 2;                  // 최대 생성 아이템 종류 수
    [SerializeField] private bool allowDuplicateLoot = false;         // 같은 후보 아이템의 중복 생성 허용 여부
    [SerializeField] private List<BoxLootOption> lootOptions = new(); // 랜덤 생성 후보 목록

    [Header("Category Loot Settings")]
    [SerializeField] private bool useCategoryLoot = true; // true면 카테고리 확률표를 사용하고, false면 기존 lootOptions를 사용합니다.
    [SerializeField] private List<BoxLootCategoryOption> categoryLootOptions = new()
    {
        new BoxLootCategoryOption
        {
            category = itemCategory.consume,
            weight = 60,
            minAmount = 1,
            maxAmount = 1,
            itemTids = new List<int> { 301, 302 }
        },
        new BoxLootCategoryOption
        {
            category = itemCategory.memory,
            weight = 30,
            minAmount = 1,
            maxAmount = 1,
            itemTids = new List<int> { 401 }
        },
        new BoxLootCategoryOption
        {
            category = itemCategory.artifact,
            weight = 10,
            minAmount = 1,
            maxAmount = 1
        }
    };

    [Header("Artifact Grade Loot Settings")]
    [SerializeField] private List<BoxArtifactLootOption> artifactLootOptions = new()
    {
        new BoxArtifactLootOption { grade = ItemGrade.normal, itemTid = 1001, weight = 55 },
        new BoxArtifactLootOption { grade = ItemGrade.uncommon, itemTid = 1002, weight = 25 },
        new BoxArtifactLootOption { grade = ItemGrade.rare, itemTid = 1003, weight = 12 },
        new BoxArtifactLootOption { grade = ItemGrade.epic, itemTid = 1004, weight = 6 },
        new BoxArtifactLootOption { grade = ItemGrade.legend, itemTid = 1005, weight = 2 }
    };

    [Header("Noise Settings")]
    // 상자를 열면 주변 적을 끌 수 있는 디코이성 소음을 함께 발생시킵니다.
    [SerializeField] private float openNoiseRange = 30.0f;
    [SerializeField] private float openNoiseDuration = 1.8f;

    private bool isOpened = false;                                  // 현재 다른 플레이어가 열어 둔 상태인지 여부
    private IItemDataRepository itemRepo;                           // TID 기반 랜덤 루트 생성을 위한 아이템 데이터 저장소
    private PlayerSight playerSight;                                // 플레이어 시야 스크립트

    /// <summary>
    /// 체스트 UI에서 읽어갈 슬롯 데이터 목록입니다.
    /// 항상 slotCapacity 개수에 맞춰 유지됩니다.
    /// </summary>
    public IReadOnlyList<BoxItemEntry> Items => items;

    /// <summary>
    /// 현재 상자가 가지는 슬롯 수를 반환합니다.
    /// </summary>
    public int SlotCount => items.Count;

    private void Awake()
    {
        itemRepo = new LocalJsonItemRepository();

        EnsureSlotCapacity();

        // 플레이어 시야 스크립트를 찾는다
        playerSight = FindObjectOfType<PlayerSight>();

        // 랜덤 루트 사용이 켜져 있고, 아직 상자가 비어 있다면 시작 시 아이템을 생성합니다.
        if (useRandomLoot && IsEmpty())
            GenerateRandomItems();
    }

    // 플레이어 시야 스크립트에 따라 렌더러 출력 상태 업데이트
    private void FixedUpdate()
    {
        if (playerSight == null) return;
        bool visible = playerSight.IsTargetInSight(playerSight.transform, transform);
        Renderer[] _rend = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in _rend)
        {
            renderer.enabled = visible;
        }
    }

/// <summary>
/// 인스펙터 우클릭 메뉴에서 랜덤 아이템 생성을 수동 실행합니다.
/// 테스트용 기능이며 현재 슬롯 구조에 맞게 다시 생성합니다.
/// </summary>
[ContextMenu("Generate Random Items")]
    private void GenerateRandomItemsFromContextMenu()
    {
        GenerateRandomItems();
    }

    /// <summary>
    /// 플레이어가 상자와 상호작용했을 때 호출됩니다.
    /// 이미 다른 쪽에서 열려 있다면 무시하고,
    /// 처음 열리는 경우 체스트 UI 오픈 이벤트를 전달합니다.
    /// </summary>
    public bool Interact(int playerID)
    {
        if (isOpened)
            return false;

        isOpened = true;
        VFXService.Instance?.Play(GameplayVFXIds.ChestOpen, transform.position, transform.rotation);

        // Presenter가 이 이벤트를 받아 체스트 UI를 열고 데이터를 바인딩합니다.
        GlobalEventBus.OnItemBoxOpened?.Invoke(this, playerID);

        // 상자 오픈 소리는 일반 발소리보다 우선순위가 높고, 추적 중인 적도 끊어낼 수 있게 설정합니다.
        NoiseSystem.Emit(NoiseType.ChestOpen, transform.position, gameObject, openNoiseRange, openNoiseDuration, true, 40);

        // 현재 프로젝트 구조에서는 상호작용 후 별도 기본 동작을 막기 위해 false를 유지합니다.
        return false;
    }

    /// <summary>
    /// 특정 슬롯의 아이템 데이터를 반환합니다.
    /// 범위를 벗어나면 null을 반환합니다.
    /// </summary>
    public BoxItemEntry GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        return items[index];
    }

    /// <summary>
    /// 특정 슬롯이 비어 있는지 확인합니다.
    /// 슬롯 자체가 없거나, itemData가 없거나, 수량이 0 이하이면 빈 슬롯으로 봅니다.
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
    /// 특정 슬롯을 빈 상태로 초기화합니다.
    /// 고정 슬롯 구조를 유지하기 위해 슬롯 자체는 삭제하지 않습니다.
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
    /// 특정 슬롯의 수량을 차감합니다.
    /// 수량이 0 이하가 되면 슬롯을 제거하지 않고 빈 슬롯으로 초기화합니다.
    /// </summary>
    public void RemoveAmount(int index, int amount)
    {
        if (index < 0 || index >= items.Count)
            return;

        if (amount <= 0 || IsSlotEmpty(index))
            return;

        items[index].amount -= amount;

        if (items[index].amount <= 0)
            ClearSlot(index);
    }

    /// <summary>
    /// 상자 전체가 비어 있는지 확인합니다.
    /// 고정 슬롯 구조이므로 items.Count가 아니라 모든 슬롯이 비었는지를 검사합니다.
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
    /// 체스트 UI가 닫힐 때 호출되어 상자의 열린 상태를 해제합니다.
    /// </summary>
    public void CloseBox()
    {
        isOpened = false;
    }

    /// <summary>
    /// 아이템을 상자 안에 넣고, 다 넣지 못한 남은 수량을 반환합니다.
    /// 먼저 같은 아이템 스택을 채우고, 남는 수량은 빈 슬롯에 넣습니다.
    /// </summary>
    public int TryAddItem(ItemData itemData, int count)
    {
        if (itemData == null || count <= 0)
            return count;

        int remain = count;

        // 1. 같은 아이템이 있는 슬롯에 먼저 누적
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

        // 2. 남은 수량을 빈 슬롯에 배치
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
    /// 특정 슬롯 하나에 아이템을 넣고, 넣지 못한 남은 수량을 반환합니다.
    /// 비어 있는 슬롯이거나 같은 아이템 스택일 때만 실제로 추가됩니다.
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

        // 다른 아이템이 들어 있으면 추가 불가
        if (slot.itemData.TID != itemData.TID)
            return count;

        // 같은 아이템이면 스택 가능한 만큼만 추가
        int canAdd = maxStack - slot.amount;

        if (canAdd <= 0)
            return count;

        int realAdd = Mathf.Min(count, canAdd);
        slot.amount += realAdd;

        return count - realAdd;
    }

    /// <summary>
    /// 랜덤 루트 후보 목록에서 아이템을 뽑아 상자 슬롯에 채웁니다.
    /// 현재 구조는 고정 슬롯 방식이므로 기존 슬롯을 모두 비운 뒤 다시 채웁니다.
    /// </summary>
    private void GenerateRandomItems()
    {
        EnsureSlotCapacity();

        int createMin = Mathf.Max(1, minCreateCount);
        int createMax = Mathf.Max(createMin, maxCreateCount);
        int createCount = Random.Range(createMin, createMax + 1);

        // 고정 슬롯 구조이므로 먼저 모든 슬롯을 비웁니다.
        ClearAllSlots();

        if (useCategoryLoot)
        {
            GenerateCategoryRandomItems(createCount);
            return;
        }

        // 유효한 후보만 따로 풀에 담습니다.
        List<BoxLootOption> pool = new List<BoxLootOption>();

        for (int i = 0; i < lootOptions.Count; i++)
        {
            if (lootOptions[i] == null || lootOptions[i].itemData == null)
                continue;

            pool.Add(lootOptions[i]);
        }

        if (pool.Count == 0)
            return;

        // 중복 불가일 때는 후보 수보다 많이 만들 수 없습니다.
        if (!allowDuplicateLoot)
            createCount = Mathf.Min(createCount, pool.Count);

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

            // 고정 슬롯 방식에 맞게 슬롯에만 추가합니다.
            TryAddItem(selectedOption.itemData, amount);

            // 중복 생성이 꺼져 있으면 같은 후보를 풀에서 제거합니다.
            if (!allowDuplicateLoot)
                pool.Remove(selectedOption);
        }
    }

    /// <summary>
    /// 카테고리 확률표를 기준으로 아이템 종류를 먼저 뽑고, 해당 카테고리의 TID를 다시 뽑아 슬롯에 채웁니다.
    /// </summary>
    private void GenerateCategoryRandomItems(int createCount)
    {
        if (itemRepo == null)
            itemRepo = new LocalJsonItemRepository();

        List<int> createdTids = new List<int>();
        int createdCount = 0;
        int attemptCount = 0;
        int maxAttemptCount = Mathf.Max(1, createCount * 10);

        while (createdCount < createCount && attemptCount < maxAttemptCount)
        {
            attemptCount++;

            BoxLootCategoryOption selectedCategory = PickRandomCategoryOption();

            if (selectedCategory == null)
                return;

            int selectedTid = PickTidByCategory(selectedCategory);

            if (selectedTid == 0)
                continue;

            if (!allowDuplicateLoot && createdTids.Contains(selectedTid))
                continue;

            ItemData selectedItem = itemRepo.GetItemDataByID(selectedTid);

            if (selectedItem == null)
                continue;

            int minAmount = Mathf.Max(1, selectedCategory.minAmount);
            int maxAmount = Mathf.Max(minAmount, selectedCategory.maxAmount);
            int amount = Random.Range(minAmount, maxAmount + 1);

            TryAddItem(selectedItem, amount);
            createdTids.Add(selectedTid);
            createdCount++;
        }
    }

    /// <summary>
    /// consume/memory/artifact 같은 카테고리 후보를 weight 값 기준으로 선택합니다.
    /// </summary>
    private BoxLootCategoryOption PickRandomCategoryOption()
    {
        if (categoryLootOptions == null || categoryLootOptions.Count == 0)
            return null;

        int totalWeight = 0;

        for (int i = 0; i < categoryLootOptions.Count; i++)
        {
            if (categoryLootOptions[i] == null)
                continue;

            totalWeight += Mathf.Max(0, categoryLootOptions[i].weight);
        }

        if (totalWeight <= 0)
            return null;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < categoryLootOptions.Count; i++)
        {
            if (categoryLootOptions[i] == null)
                continue;

            currentWeight += Mathf.Max(0, categoryLootOptions[i].weight);

            if (randomValue < currentWeight)
                return categoryLootOptions[i];
        }

        return null;
    }

    /// <summary>
    /// 선택된 카테고리 후보에서 실제 아이템 TID를 뽑습니다.
    /// 아티팩트는 등급 확률표를 한 번 더 거쳐 TID를 결정합니다.
    /// </summary>
    private int PickTidByCategory(BoxLootCategoryOption categoryOption)
    {
        if (categoryOption == null)
            return 0;

        if (categoryOption.category == itemCategory.artifact)
            return PickArtifactTidByGrade();

        return PickRandomTid(categoryOption.itemTids);
    }

    /// <summary>
    /// TID 후보 목록에서 같은 확률로 하나를 선택합니다.
    /// </summary>
    private int PickRandomTid(List<int> tids)
    {
        if (tids == null || tids.Count == 0)
            return 0;

        int index = Random.Range(0, tids.Count);
        return tids[index];
    }

    /// <summary>
    /// 아티팩트가 선택된 경우 등급별 weight를 기준으로 실제 아티팩트 TID를 선택합니다.
    /// </summary>
    private int PickArtifactTidByGrade()
    {
        if (artifactLootOptions == null || artifactLootOptions.Count == 0)
            return 0;

        int totalWeight = 0;

        for (int i = 0; i < artifactLootOptions.Count; i++)
        {
            if (artifactLootOptions[i] == null)
                continue;

            totalWeight += Mathf.Max(0, artifactLootOptions[i].weight);
        }

        if (totalWeight <= 0)
            return 0;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < artifactLootOptions.Count; i++)
        {
            if (artifactLootOptions[i] == null)
                continue;

            currentWeight += Mathf.Max(0, artifactLootOptions[i].weight);

            if (randomValue < currentWeight)
                return artifactLootOptions[i].itemTid;
        }

        return 0;
    }

    /// <summary>
    /// 후보 목록에서 weight 값을 기준으로 랜덤 아이템 하나를 선택합니다.
    /// weight가 클수록 선택될 확률이 높습니다.
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

        // 예외 상황이더라도 마지막 후보를 반환해 null을 피합니다.
        return options[options.Count - 1];
    }

    /// <summary>
    /// 내부 슬롯 리스트를 slotCapacity 크기에 맞게 보정합니다.
    /// 부족하면 빈 슬롯을 추가하고, 넘치면 뒤에서부터 제거합니다.
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
    /// 모든 슬롯을 빈 상태로 초기화합니다.
    /// 슬롯 개수 자체는 유지합니다.
    /// </summary>
    private void ClearAllSlots()
    {
        for (int i = 0; i < items.Count; i++)
        {
            ClearSlot(i);
        }
    }

    /// <summary>
    /// 비어 있는 슬롯 데이터를 하나 생성합니다.
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
