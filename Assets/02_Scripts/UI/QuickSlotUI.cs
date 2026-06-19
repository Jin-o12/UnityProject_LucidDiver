using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[System.Serializable]
public struct slotData
{
    public Image slotIcon;
    public TMP_Text itemStack;
};

public class QuickSlotUI : MonoBehaviour
{
    [Header("장착 무기 UI")]
    [SerializeField] Image weaponImage;

    [Header("퀵슬롯 아이템 UI")]
    [SerializeField] slotData[] soltDataList;

    private void OnEnable()
    {
        /// 이벤트 구독 ///
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
    }

    public void initialize()
    {
        weaponImage.enabled = false;
        for(int i=0; i<soltDataList.Length; i++)
        {
            soltDataList[i].slotIcon.enabled = false;
            soltDataList[i].itemStack.text = "";
        }
    }
}
