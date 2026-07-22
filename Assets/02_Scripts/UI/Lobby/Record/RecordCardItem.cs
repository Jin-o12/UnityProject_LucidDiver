using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RecordCardItem : MonoBehaviour
{
    [SerializeField] private Button recordCardButton;
    [SerializeField] private CanvasGroup recordCardCanvasGroup;
    [SerializeField] private TextMeshProUGUI textRecordState;
    [SerializeField] private TextMeshProUGUI textRecordTitle;
    [SerializeField] private TextMeshProUGUI textRecordDesc;
    [SerializeField] private TextMeshProUGUI textOpenRecord;
    [SerializeField] private TextMeshProUGUI textNewBadge;

    public void Setup(bool isUnlocked, string title, string desc, int recordIndex, bool hasNewBadge, UnityAction onClick)
    {
        // 기록 상태 표시
        if (textRecordState != null)
            textRecordState.text = isUnlocked ? $"[OPEN] 기록 {recordIndex:D2}" : $"[LOCK] 기록 {recordIndex:D2}";

        // 기록 제목 표시
        if (textRecordTitle != null)
            textRecordTitle.text = isUnlocked ? title : "???";

        // 기록 설명 표시
        if (textRecordDesc != null)
            textRecordDesc.text = desc;

        // 기록 보기 문구 표시
        if (textOpenRecord != null)
            textOpenRecord.text = isUnlocked ? "기록 보기" : "잠김";

        // 버튼 활성화
        if (recordCardButton != null)
        {
            recordCardButton.interactable = isUnlocked;
            recordCardButton.onClick.RemoveAllListeners();
            if (isUnlocked)
            {
                recordCardButton.onClick.AddListener(onClick);
            }
        }

        // 투명도 처리
        if (recordCardCanvasGroup != null)
        {
            recordCardCanvasGroup.alpha = isUnlocked ? 1f : 0.55f;
            recordCardCanvasGroup.interactable = isUnlocked;
            recordCardCanvasGroup.blocksRaycasts = isUnlocked;
        }

        // NEW 배지 표시
        if (textNewBadge != null)
            textNewBadge.gameObject.SetActive(isUnlocked && hasNewBadge);
    }
}
