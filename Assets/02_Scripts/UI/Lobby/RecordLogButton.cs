using System;
using UnityEngine;
using UnityEngine.UI;

public class RecordLogButton : MonoBehaviour
{
    [SerializeField] Button buttonClose;       // 대화 기록 닫기 버튼
    public Action onClickAction;                // 버튼 클릭 시 로컬 이벤트

    private void OnEnable()
    {
        buttonClose.onClick.AddListener(OnClickButton);
    }

    private void OnDisable()
    {
        buttonClose.onClick.RemoveListener(OnClickButton);
    }

    public void OnClickButton()
    {
        onClickAction?.Invoke();
    }
}
