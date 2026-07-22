using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordAnswerButton : MonoBehaviour
{
    [SerializeField] Button buttonAnswer;       // 대답 선택지 버튼
    [SerializeField] TMP_Text textAnswer;       // 대답 선택지에 출력 될 텍스트
    public Action onClickAction;                // 버튼 클릭 시 로컬 이벤트

    private void OnEnable()
    {
        buttonAnswer.onClick.AddListener(OnClickButton);
    }

    private void OnDisable()
    {
        buttonAnswer.onClick.RemoveListener(OnClickButton);
    }

    public void PrintText(string _text)
    {
        textAnswer.text = _text;
    }

    
    /* 버튼이 클릭 되었을 시 로컬 이벤트로 처리 */
    public void OnClickButton()
    {
        onClickAction?.Invoke();
    }
}
