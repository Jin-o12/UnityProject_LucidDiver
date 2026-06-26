using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �÷��̾ ��ȣ�ۿ��� �� �ִ� �����̳� �ڽ�
/// ���ڴ� ������ ���� ������ ������,
/// �������� ������ ���� ��ü�� �������� �ʰ� �� ĭ���� �����ȴ�.
/// </summary>
public class ItemBox : MonoBehaviour, IInteractable
{
    [Header("Container Settings")]
    [SerializeField] private int slotCapacity = 8;                 // ���ڰ� ���� ���� ���� ����
    [SerializeField] private List<BoxItemEntry> items = new();     // ���� ���� ������ ���

    [Header("Random Loot Settings")]
    [SerializeField] private bool useRandomLoot = true;               // ���� �� ���� ������ ���� ��� ����
    [SerializeField] private int minCreateCount = 1;                  // �ּ� ���� ������ ���� ��
    [SerializeField] private int maxCreateCount = 2;                  // �ִ� ���� ������ ���� ��
    [SerializeField] private bool allowDuplicateLoot = false;         // ���� ������ �ߺ� ���� ��� ����
    [SerializeField] private List<BoxLootOption> lootOptions = new(); // ���� �ĺ� ������ ���

    private bool isOpened = false;                                    // ���� ���ڰ� ���� �ִ��� ����

    /// <summary>
    /// ü��Ʈ UI�� �о ���� ���
    /// ���� ���� �׻� slotCapacity�� �����ϰ� �����ȴ�.
    /// </summary>
    public IReadOnlyList<BoxItemEntry> Items => items;

    /// <summary>
    /// ���� ���ڰ� ������ �� ���� ����
    /// </summary>
    public int SlotCount => items.Count;

    private void Awake()
    {
        EnsureSlotCapacity();

        // ���� ���� ��带 ����ϰ�,
        // ���� ���� ���� ������ ��� ���� ���� ���� �� �� �� ä���.
        if (useRandomLoot && IsEmpty())
            GenerateRandomItems();
    }

    private void OnValidate()
    {
        EnsureSlotCapacity();
    }

    /// <summary>
    /// �ν����� ��Ŭ�� �޴����� ���� �������� �ٽ� ������ �� ���
    /// </summary>
    [ContextMenu("Generate Random Items")]
    private void GenerateRandomItemsFromContextMenu()
    {
        EnsureSlotCapacity();
        GenerateRandomItems();
    }

    /// <summary>
    /// �÷��̾ ���ڿ� ��ȣ�ۿ����� �� ȣ��
    /// �̹� ���� ������ �ߺ����� ���� �ʴ´�.
    /// </summary>
    public bool Interact(int playerID)
    {
        if (isOpened)
            return false;

        isOpened = true;

        // Presenter�� ü��Ʈ UI�� �� �� �ֵ��� �̺�Ʈ ����
        GlobalEventBus.OnItemBoxOpened?.Invoke(this, playerID);

        // ���ڴ� ������� �����Ƿ� false ��ȯ
        return false;
    }

    /// <summary>
    /// ������ ���� �ε����� �����͸� ��ȯ
    /// </summary>
    public BoxItemEntry GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        return items[index];
    }

    /// <summary>
    /// ������ ������ ��� �ִ��� Ȯ��
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
    /// ���� �ϳ��� ������ ���
    /// ���� ��ü�� �������� �ʴ´�.
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
    /// ���� �ϳ����� ������ ���ҽ�Ŵ
    /// ������ 0 ���ϰ� �Ǹ� �� �������� �����.
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
    /// ���� ��ü�� ��� �ִ��� Ȯ��
    /// ���� ������ 0������ �ƴ϶�, ��� ������ ��������� ����.
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
    /// ���� UI�� ���� �� �ٽ� �� �� �ֵ��� ���¸� ����
    /// </summary>
    public void CloseBox()
    {
        isOpened = false;
    }

    /// <summary>
    /// �������� ���� �ȿ� �ְ�, �� �� ���� ������ ��ȯ
    /// ���� ���� ������ ������ ä���, �� ���� �� ĭ�� �ִ´�.
    /// </summary>
    public int TryAddItem(ItemData itemData, int count)
    {
        if (itemData == null || count <= 0)
            return count;

        int remain = count;

        // 1. ���� ���� �������� �ִ� ���Կ� ����
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

        // 2. ���� ������ �� ���Կ� �߰�
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
    /// Ư�� ���Կ� �������� �ְ�, �� �� ���� ������ ��ȯ
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

        // �� �����̸� ���� ��ġ
        if (IsSlotEmpty(slotIndex))
        {
            int addAmount = Mathf.Min(count, maxStack);

            slot.itemData = itemData;
            slot.amount = addAmount;

            return count - addAmount;
        }

        // ���� �������̸� ���� ����
        if (slot.itemData.TID == itemData.TID)
        {
            int canAdd = maxStack - slot.amount;

            if (canAdd <= 0)
                return count;

            int addAmount = Mathf.Min(count, canAdd);
            slot.amount += addAmount;

            return count - addAmount;
        }

        // �ٸ� �������� �̹� ������ ���� ����
        return count;
    }

    /// <summary>
    /// ���� �ĺ� ��Ͽ��� �������� �̾� ���� �� ���Կ� ä��
    /// ���� ������ �������� �ʰ� ���븸 ��� �� �ٽ� ä���.
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

        // �ߺ� �Ұ��� ���� �ĺ� �� / ���� ���� ���� �ʵ��� ����
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

            // ���� �ȿ� ������ ������ �ֱ�
            TryAddItem(selectedOption.itemData, amount);

            if (!allowDuplicateLoot)
                pool.Remove(selectedOption);
        }
    }

    /// <summary>
    /// ����ġ �������� ���� �ĺ� �ϳ��� ����
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
    /// ������ ���� ������ slotCapacity�� ����
    /// �� ���Ե� ���� ��Ʈ���� �����Ѵ�.
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
    /// ���� ���� ��� ������ �� ĭ���� �ʱ�ȭ
    /// ���� ��ü�� �������� �ʴ´�.
    /// </summary>
    private void ClearAllSlots()
    {
        for (int i = 0; i < items.Count; i++)
        {
            ClearSlot(i);
        }
    }

    /// <summary>
    /// �� ���� ��Ʈ���� ����
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