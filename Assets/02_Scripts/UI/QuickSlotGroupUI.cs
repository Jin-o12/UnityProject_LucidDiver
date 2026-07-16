using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

[System.Serializable]
public struct slotData
{
    public Image slotIcon;
    public TMP_Text itemStack;
};

public class QuickSlotGroupUI : MonoBehaviour
{
    [Header("장착 무기 UI")]
    [SerializeField] Image weaponImage;

    [Header("퀵슬롯 아이템 UI")]
    [SerializeField] QuickSlotUI[] slotDataList;

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnQuickSlotChanged += UpdateSlot;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnQuickSlotChanged -= UpdateSlot;
    }

    public void InitializeSlots()
    {
        for(int i=0; i<slotDataList.Length; i++)
        {
            slotDataList[i].Initialize(i);
        }
    }

    public void UpdateSlot(int index, Sprite icon, int count)
    {
        var slotUI = slotDataList[index].GetComponent<QuickSlotUI>();
        slotUI.UpdateSlot(count, icon);
    }
}
