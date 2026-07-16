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
            if (slotDataList[i] == null)
            {
                Debug.LogWarning($"QuickSlotGroupUI: slotDataList[{i}]가 연결되지 않아 해당 퀵슬롯 초기화를 건너뜁니다.", this);
                continue;
            }

            slotDataList[i].Initialize(i);
        }
    }

    public void UpdateSlot(int index, Sprite icon, int count)
    {
        if (slotDataList == null || index < 0 || index >= slotDataList.Length || slotDataList[index] == null)
        {
            Debug.LogWarning($"QuickSlotGroupUI: 유효하지 않은 퀵슬롯 인덱스입니다. index={index}", this);
            return;
        }

        var slotUI = slotDataList[index].GetComponent<QuickSlotUI>();
        if (slotUI == null)
        {
            Debug.LogWarning($"QuickSlotGroupUI: slotDataList[{index}]에 QuickSlotUI 컴포넌트가 없습니다.", this);
            return;
        }

        slotUI.UpdateSlot(count, icon);
    }
}
