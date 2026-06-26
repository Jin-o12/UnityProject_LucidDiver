using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �÷��̾ ��ȣ�ۿ��� �� �ִ� �����̳� �ڽ��Դϴ�.
/// ���� �ȿ��� ������ �ű� �� �ִ� ������ �����Ͱ� ��� ������,
/// �ʿ� �� ���� ������ �������� �������� ������ ä�� �ֽ��ϴ�.
/// </summary>
public class ItemBox : MonoBehaviour, IInteractable
{
    [Header("Container Contents")]
    [SerializeField] private List<BoxItemEntry> items = new();

    [Header("Random Loot Settings")]
    [SerializeField] private bool useRandomLoot = true;              // ���� �� ���� ������ ���� ��� ����
    [SerializeField] private int minCreateCount = 1;                // �ּ� ���� ������ ���� ��
    [SerializeField] private int maxCreateCount = 2;                // �ִ� ���� ������ ���� ��
    [SerializeField] private bool allowDuplicateLoot = false;       // ���� ������ �ߺ� ���� ��� ����
    [SerializeField] private List<BoxLootOption> lootOptions = new(); // ���� �ĺ� ������ ���

    private bool isOpened = false;

    /// <summary>
    /// ���� ���� �ȿ� ��� �ִ� ������ ����Դϴ�.
    /// ChestUI�� �� �����͸� �о ������ �����մϴ�.
    /// </summary>
    public IReadOnlyList<BoxItemEntry> Items => items;

    private void Awake()
    {
        // ���� ���� ��带 ����ϰ�,
        // ���� ���� ������ ��� ���� ���� ���� �� �� �� �������� ä��ϴ�.
        if (useRandomLoot && items.Count == 0)
            GenerateRandomItems();
    }

    /// <summary>
    /// �ν����� ��Ŭ�� �޴����� �������� ���� �������� �ٽ� ������ �� ����մϴ�.
    /// �׽�Ʈ�� �� ���ϰ� Ȯ���� �� �ֵ��� �߰��մϴ�.
    /// </summary>
    [ContextMenu("Generate Random Items")]
    private void GenerateRandomItemsFromContextMenu()
    {
        GenerateRandomItems();
    }

    /// <summary>
    /// �÷��̾ ���ڿ� ��ȣ�ۿ����� �� ȣ��˴ϴ�.
    /// �̹� ���� ������ �ߺ����� ���� �ʽ��ϴ�.
    /// </summary>
    public bool Interact(int playerID)
    {
        // �̹� ���� ������ �ٽ� ���� ����
        if (isOpened)
            return false;

        isOpened = true;

        // ���� ���� �̺�Ʈ�� �����Ͽ� Presenter�� UI�� ������ ��û
        GlobalEventBus.OnItemBoxOpened?.Invoke(this, playerID);

        // ���ڴ� ������� �����Ƿ� false ��ȯ
        return false;
    }

    /// <summary>
    /// ������ �ε����� ���� ������ �����͸� ��ȯ�մϴ�.
    /// </summary>
    public BoxItemEntry GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        return items[index];
    }

    /// <summary>
    /// ���� �� Ư�� �������� ������ ���ҽ�ŵ�ϴ�.
    /// ������ 0 ���ϰ� �Ǹ� �ش� ��Ʈ���� �����մϴ�.
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
    /// ���� �ȿ� �� �̻� �������� ������ Ȯ���մϴ�.
    /// </summary>
    public bool IsEmpty()
    {
        return items.Count == 0;
    }

    /// <summary>
    /// ���� UI�� ���� �� �ٽ� �� �� �ֵ��� ���¸� �����մϴ�.
    /// </summary>
    public void CloseBox()
    {
        isOpened = false;
    }

    /// <summary>
    /// ���� �ĺ� ��Ͽ��� 1~2���� �������� �̾� ���� �������� ä��ϴ�.
    /// ���� ������ ����� ���� �����մϴ�.
    /// </summary>
    private void GenerateRandomItems()
    {
        // �߸��� ���� ����
        int createMin = Mathf.Max(1, minCreateCount);
        int createMax = Mathf.Max(createMin, maxCreateCount);

        // ��ȿ�� �ĺ��� ���� �����ϴ�.
        List<BoxLootOption> pool = new List<BoxLootOption>();

        for (int i = 0; i < lootOptions.Count; i++)
        {
            if (lootOptions[i] == null || lootOptions[i].itemData == null)
                continue;

            pool.Add(lootOptions[i]);
        }

        // �ĺ��� ������ �������� ����
        if (pool.Count == 0)
            return;

        items.Clear();

        // �̹� ���ڿ� �� ������ �������� ������ ����
        int createCount = Random.Range(createMin, createMax + 1);

        for (int i = 0; i < createCount; i++)
        {
            // �ߺ� ���� ���¿��� �ĺ��� �� �����ϸ� ����
            if (pool.Count == 0)
                break;

            BoxLootOption selectedOption = PickRandomOption(pool);

            if (selectedOption == null || selectedOption.itemData == null)
                continue;

            // ���� ������ �߸� ���͵� �ּ� 1�� �̻� �����ǵ��� ����
            int minAmount = Mathf.Max(1, selectedOption.minAmount);
            int maxAmount = Mathf.Max(minAmount, selectedOption.maxAmount);
            int amount = Random.Range(minAmount, maxAmount + 1);

            // ���� ���� ���� ����Ʈ�� �߰�
            items.Add(new BoxItemEntry
            {
                itemData = selectedOption.itemData,
                amount = amount
            });

            // ���� �������� �� ���ڿ��� �ߺ� �������� �������� ����
            if (!allowDuplicateLoot)
                pool.Remove(selectedOption);
        }
    }

    /// <summary>
    /// ����ġ(weight)�� �������� �ĺ� ������ �ϳ��� ���� �����մϴ�.
    /// weight ���� Ŭ���� ���õ� Ȯ���� �������ϴ�.
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

        // ���� ��Ȳ ������ ������ ��ȯ
        return options[options.Count - 1];
    }
}