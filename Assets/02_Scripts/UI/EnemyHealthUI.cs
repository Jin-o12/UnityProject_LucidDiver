/// <summary>
/// 적 체력 바 UI 업데이트 클래스
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("체력 UI")]
    [SerializeField] Image hpBar;
    [SerializeField] TMP_Text hpText;

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnEnemyHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnEnemyHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(int id, float currentHp, float maxHp)
    {
        // 특정 id를 가진 개체일 경우 아래의 과정 수행, 그렇지 않다면 수행하지 않는 로직 차후 추가
        hpBar.fillAmount = currentHp/maxHp;
        hpText.text = $"{currentHp}/{maxHp}";
    }
}
