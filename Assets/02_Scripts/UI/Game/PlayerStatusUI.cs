/// <summary>
/// 플레이어 체력 바 UI 업데이트 클래스
/// [26.06.16_강다영] 현재 플레이어가 한명임을 가정하고 코드가 작성 되었으므로 이후 멀티 환경에 맞게 변경 해야할 것
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("플레이어 초상화 UI")]
    [SerializeField] Image playerImage;

    [Header("체력 UI")]
    [SerializeField] Image hpBar;
    [SerializeField] TMP_Text hpText;

    [Header("마나 UI")]
    [SerializeField] Image mpBar;
    [SerializeField] TMP_Text mpText;

    [Header("타이머 UI")]
    [SerializeField] TMP_Text timerText;

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnPlayerHealthChanged += UpdateHealthBar;
        GlobalEventBus.OnPlayerManaChanged += UpdateManaBar;
        GlobalEventBus.OnTimerChanged += UpdateTimer;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnPlayerHealthChanged -= UpdateHealthBar;
        GlobalEventBus.OnPlayerManaChanged -= UpdateManaBar;
        GlobalEventBus.OnTimerChanged -= UpdateTimer;
    }

    private void UpdateHealthBar(float _currentHp, float _maxHp)
    {
        hpBar.fillAmount = _maxHp > 0f ? Mathf.Clamp01(_currentHp / _maxHp) : 0f;
        hpText.text = $"{_currentHp}/{_maxHp}";
    }

    private void UpdateManaBar(float _currentMp, float _maxMp)
    {
        mpBar.fillAmount = _maxMp > 0f ? Mathf.Clamp01(_currentMp / _maxMp) : 0f;
        mpText.text = $"{Math.Round(_currentMp, 0)}/{_maxMp}";
    }

    private void UpdateTimer(float _currentTimer)
    {
        int _currentMinute = (int) _currentTimer / 60;
        int _currentSecond = (int) _currentTimer % 60;
        timerText.text = $"{_currentMinute:00}:{_currentSecond:00}";
    }
}
