/// <summary>
/// 플레이어 체력 바 UI 업데이트 클래스
/// [26.06.16_강다영] 현재 플레이어가 한명임을 가정하고 코드가 작성 되었으므로 이후 멀티 환경에 맞게 변경 해야할 것
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("체력 UI")]
    [SerializeField] Image hpBar;
    [SerializeField] TMP_Text hpText;

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnPlayerHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnPlayerHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float currentHp, float maxHp)
    {
        hpBar.fillAmount = currentHp/maxHp;
        hpText.text = $"{currentHp}/{maxHp}";
    }
}
