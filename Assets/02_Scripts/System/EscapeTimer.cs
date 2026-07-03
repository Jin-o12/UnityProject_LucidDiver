using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EscapeTimer : MonoBehaviour
{
    [SerializeField] private Image timerImage;
    [SerializeField] public TMP_Text timerText;
    private float timerTime;
    private WaitForSeconds oneSec = new WaitForSeconds(1f);

    private void OnEnable()
    {
        // 부모 오브젝트의 ExitPoint에서 timerOn 이벤트를 구독
        GetComponentInParent<ExitPoint>().timerOn += EscapeTimer_timerOn;    
    }

    private void OnDisable()
    {
        GetComponentInParent<ExitPoint>().timerOn -= EscapeTimer_timerOn;
    }

    // 타이머 초기 값 설정
    private void EscapeTimer_timerOn(float _startTime)
    {
        timerTime = _startTime;
        timerText.text = $"{_startTime:00}";
    }

    private void FixedUpdate()
    {
        timerTime -= Time.fixedDeltaTime;
        timerText.text = $"{Mathf.CeilToInt(timerTime):00}";
    }
}
