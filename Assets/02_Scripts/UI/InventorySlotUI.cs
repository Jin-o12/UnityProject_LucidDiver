/// <summary>
/// 아이템 슬롯 하나의 역할을 수행합니다
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class InventorySlotUI : MonoBehaviour
{
    [Header("슬롯 내 요소")]
    [SerializeField] private Image itemImg;
    [SerializeField] private TMP_Text itemStack;

    /* 슬롯 초기화 */
    public void Initialize()
    {
        itemImg.enabled = false;
        itemStack.text = "";
    }

    /* 해당 슬롯의 UI를 변경하는 함수 (아이템 갯수, 스프라이트 이미지) */
    public void UpdateSlot(int _stack, Sprite _sprite)
    {
        if(_stack==0 || _sprite==null) return;
        itemStack.text = $"{_stack}";
        itemImg.sprite = _sprite;
        itemImg.enabled = true;
    }
}
