using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChestSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image itemImg;
    [SerializeField] private TMP_Text itemStack;

    private ChestUI chestUI;
    private int slotIndex;

    public void Initialize(ChestUI owner, int index)
    {
        chestUI = owner;
        slotIndex = index;
        UpdateSlot(0, null);
    }

    public void UpdateSlot(int stack, Sprite sprite)
    {
        if (stack <= 0 || sprite == null)
        {
            itemImg.enabled = false;
            itemStack.text = "";
            return;
        }

        itemImg.enabled = true;
        itemImg.sprite = sprite;
        itemStack.text = stack.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            chestUI.TryMoveToInventory(slotIndex);
    }
}