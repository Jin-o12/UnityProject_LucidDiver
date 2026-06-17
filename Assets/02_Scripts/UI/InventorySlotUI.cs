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
    private InventorySlotData itemData;

    [Header("슬롯 내 요소")]
    [SerializeField] private Image itemImg;
    [SerializeField] private TMP_Text itemStack;

    /* 슬롯 초기화 */
    public void Initialize()
    {
        itemImg.enabled = false;
        itemStack.text = "";
    }

    /* 해당 슬롯의 UI를 변경하는 함수 (스프라이트 이미지, 아이템 갯수) */
    public void SetStackCount(int stack)
    {
        itemStack.text = $"{stack}";
    }

    /* 아이콘 이미지 설정 */
    public void SetIcon(AssetReferenceSprite _ref)
    {
        // 스프라이트 이미지 주소의 유효성 검사
        if (_ref == null || !_ref.RuntimeKeyIsValid()) return;
        // 비동기 로드
        AsyncOperationHandle<Sprite> loadIcon = Addressables.LoadAssetAsync<Sprite>(_ref);
        loadIcon.Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 스프라이트 적용
                itemImg.sprite = handle.Result;
                itemImg.enabled = true;
            }
            else
            {
                Debug.LogError("이미지 로드 실패");
            }
        };
    }

}
