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

    }

    private void OnDisable()
    {

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
}
