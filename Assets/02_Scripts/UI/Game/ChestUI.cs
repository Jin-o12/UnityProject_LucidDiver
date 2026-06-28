using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ChestUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private List<GameObject> slotsObj = new();
    [SerializeField] private Button closeButton;

    private ItemBox itemBox;
    private PlayerInventory playerInventory;
    private Action onCloseRequested;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }

    public void Bind(ItemBox box, PlayerInventory inventory, Action closeRequested = null)
    {
        itemBox = box;
        playerInventory = inventory;
        onCloseRequested = closeRequested;

        CreateSlots(itemBox.Items.Count);
        RefreshAll();
    }

    public void CreateSlots(int count)
    {
        foreach (var obj in slotsObj)
            Destroy(obj);

        slotsObj.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            ChestSlotUI slotUI = newSlot.GetComponent<ChestSlotUI>();
            slotUI.Initialize(this, i);
            slotsObj.Add(newSlot);
        }
    }

    public void RefreshAll()
    {
        for (int i = 0; i < slotsObj.Count; i++)
        {
            ChestSlotUI slotUI = slotsObj[i].GetComponent<ChestSlotUI>();
            BoxItemEntry entry = itemBox.GetItem(i);

            if (entry == null || entry.itemData == null)
                slotUI.UpdateSlot(0, null);
            else
                LoadSlotIcon(slotUI, entry);
        }
    }

    public void TryMoveToInventory(int slotIndex)
    {
        BoxItemEntry entry = itemBox.GetItem(slotIndex);
        if (entry == null || entry.itemData == null || entry.amount <= 0)
            return;

        int remain = playerInventory.AddItem(entry.itemData, entry.amount);
        int moved = entry.amount - remain;

        if (moved <= 0)
            return;

        itemBox.RemoveAmount(slotIndex, moved);

        CreateSlots(itemBox.Items.Count);
        RefreshAll();

        if (itemBox.IsEmpty())
            CloseUI();
    }

    public void CloseUI()
    {
        onCloseRequested?.Invoke();
    }

    private void LoadSlotIcon(ChestSlotUI slotUI, BoxItemEntry entry)
    {
        if (slotUI == null || entry == null || entry.itemData == null)
            return;

        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(entry.itemData.icon);
        handle.Completed += operation =>
        {
            if (slotUI == null)
                return;

            if (operation.Status == AsyncOperationStatus.Succeeded)
                slotUI.UpdateSlot(entry.amount, operation.Result);
            else
                slotUI.UpdateSlot(entry.amount, null);

            Addressables.Release(operation);
        };
    }
}
