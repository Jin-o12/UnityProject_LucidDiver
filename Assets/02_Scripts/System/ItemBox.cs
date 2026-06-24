using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 상호작용할 수 있는 컨테이너 박스입니다.
/// 상자 안에는 실제로 옮길 수 있는 아이템 데이터가 들어 있으며,
/// 필요 시 시작 시점에 랜덤으로 아이템을 생성해 채워 넣습니다.
/// </summary>
public class ItemBox : MonoBehaviour, IInteractable
{
    [Header("Container Contents")]
    [SerializeField] private List<BoxItemEntry> items = new();

    [Header("Random Loot Settings")]
    [SerializeField] private bool useRandomLoot = true;              // 시작 시 랜덤 아이템 생성 사용 여부
    [SerializeField] private int minCreateCount = 1;                // 최소 생성 아이템 종류 수
    [SerializeField] private int maxCreateCount = 2;                // 최대 생성 아이템 종류 수
    [SerializeField] private bool allowDuplicateLoot = false;       // 같은 아이템 중복 생성 허용 여부
    [SerializeField] private List<BoxLootOption> lootOptions = new(); // 랜덤 후보 아이템 목록

    private bool isOpened = false;

    /// <summary>
    /// 현재 상자 안에 들어 있는 아이템 목록입니다.
    /// ChestUI가 이 데이터를 읽어서 슬롯을 생성합니다.
    /// </summary>
    public IReadOnlyList<BoxItemEntry> Items => items;

    private void Awake()
    {
        // 랜덤 생성 모드를 사용하고,
        // 아직 상자 내용이 비어 있을 때만 시작 시 한 번 아이템을 채웁니다.
        if (useRandomLoot && items.Count == 0)
            GenerateRandomItems();
    }

    /// <summary>
    /// 인스펙터 우클릭 메뉴에서 수동으로 랜덤 아이템을 다시 생성할 때 사용합니다.
    /// 테스트할 때 편하게 확인할 수 있도록 추가합니다.
    /// </summary>
    [ContextMenu("Generate Random Items")]
    private void GenerateRandomItemsFromContextMenu()
    {
        GenerateRandomItems();
    }

    /// <summary>
    /// 플레이어가 상자와 상호작용했을 때 호출됩니다.
    /// 이미 열려 있으면 중복으로 열지 않습니다.
    /// </summary>
    public bool Interact(int playerID)
    {
        // 이미 열려 있으면 다시 열지 않음
        if (isOpened)
            return false;

        isOpened = true;

        // 상자 열림 이벤트를 발행하여 Presenter가 UI를 열도록 요청
        GlobalEventBus.OnItemBoxOpened?.Invoke(this, playerID);

        // 상자는 사라지지 않으므로 false 반환
        return false;
    }

    /// <summary>
    /// 지정한 인덱스의 상자 아이템 데이터를 반환합니다.
    /// </summary>
    public BoxItemEntry GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        return items[index];
    }

    /// <summary>
    /// 상자 안 특정 아이템의 수량을 감소시킵니다.
    /// 수량이 0 이하가 되면 해당 엔트리를 제거합니다.
    /// </summary>
    public void RemoveAmount(int index, int amount)
    {
        if (index < 0 || index >= items.Count)
            return;

        items[index].amount -= amount;

        if (items[index].amount <= 0)
            items.RemoveAt(index);
    }

    /// <summary>
    /// 상자 안에 더 이상 아이템이 없는지 확인합니다.
    /// </summary>
    public bool IsEmpty()
    {
        return items.Count == 0;
    }

    /// <summary>
    /// 상자 UI를 닫을 때 다시 열 수 있도록 상태를 해제합니다.
    /// </summary>
    public void CloseBox()
    {
        isOpened = false;
    }

    /// <summary>
    /// 랜덤 후보 목록에서 1~2개의 아이템을 뽑아 상자 내용으로 채웁니다.
    /// 기존 내용은 지우고 새로 생성합니다.
    /// </summary>
    private void GenerateRandomItems()
    {
        // 잘못된 설정 방지
        int createMin = Mathf.Max(1, minCreateCount);
        int createMax = Mathf.Max(createMin, maxCreateCount);

        // 유효한 후보만 따로 모읍니다.
        List<BoxLootOption> pool = new List<BoxLootOption>();

        for (int i = 0; i < lootOptions.Count; i++)
        {
            if (lootOptions[i] == null || lootOptions[i].itemData == null)
                continue;

            pool.Add(lootOptions[i]);
        }

        // 후보가 없으면 생성하지 않음
        if (pool.Count == 0)
            return;

        items.Clear();

        // 이번 상자에 몇 종류의 아이템을 넣을지 결정
        int createCount = Random.Range(createMin, createMax + 1);

        for (int i = 0; i < createCount; i++)
        {
            // 중복 금지 상태에서 후보를 다 소진하면 종료
            if (pool.Count == 0)
                break;

            BoxLootOption selectedOption = PickRandomOption(pool);

            if (selectedOption == null || selectedOption.itemData == null)
                continue;

            // 수량 범위가 잘못 들어와도 최소 1개 이상 생성되도록 보정
            int minAmount = Mathf.Max(1, selectedOption.minAmount);
            int maxAmount = Mathf.Max(minAmount, selectedOption.maxAmount);
            int amount = Random.Range(minAmount, maxAmount + 1);

            // 실제 상자 내용 리스트에 추가
            items.Add(new BoxItemEntry
            {
                itemData = selectedOption.itemData,
                amount = amount
            });

            // 같은 아이템을 한 상자에서 중복 생성하지 않으려면 제거
            if (!allowDuplicateLoot)
                pool.Remove(selectedOption);
        }
    }

    /// <summary>
    /// 가중치(weight)를 기준으로 후보 아이템 하나를 랜덤 선택합니다.
    /// weight 값이 클수록 선택될 확률이 높아집니다.
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

        // 예외 상황 방지용 마지막 반환
        return options[options.Count - 1];
    }
}