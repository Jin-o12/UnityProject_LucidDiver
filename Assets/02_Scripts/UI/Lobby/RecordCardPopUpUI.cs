using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordCardPopUpUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI textMemoryLogTitle;    // {기록 제목 텍스트}
    [SerializeField] private TextMeshProUGUI textMemoryLogBody;     // {기록 본문 텍스트}

    [Header("Buttons")]
    [SerializeField] private Button buttonClose;                    // {닫기 버튼}

    private void OnEnable()
    {
        // {닫기 버튼 클릭 이벤트를 등록한다}
        buttonClose.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        // {닫기 버튼 클릭 이벤트를 해제한다}
        buttonClose.onClick.RemoveListener(Close);
    }

    public void SetData(string title, string body)
    {
        // {기록 제목을 표시한다}
        if (textMemoryLogTitle != null)
            textMemoryLogTitle.text = title;

        // {기록 본문을 표시한다}
        if (textMemoryLogBody != null)
            textMemoryLogBody.text = body;
    }

    private void Close()
    {
        // {현재 최상단 UI 닫기 이벤트를 호출한다}
        GlobalEventBus.OnOpenRecordUI?.Invoke();
    }
}
