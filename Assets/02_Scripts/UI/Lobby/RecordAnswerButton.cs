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

    public Action onClickAction;

    public void OnClickButton()
    {
        onClickAction?.Invoke();
    }
}
