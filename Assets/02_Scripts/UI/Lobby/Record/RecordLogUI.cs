using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordLogUI : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] GameObject logTextBox;

    [Header("Container")]
    [SerializeField] Transform contentContainer; // logTextBox를 생성할 부모 컨테이너

    private List<GameObject> activeLogs = new List<GameObject>();

    public void ShowLog(IRecordRepository repo, int charTID, int reqLevel, int maxIndex)
    {
        // 기존에 생성된 로그 텍스트 박스 삭제
        foreach (var log in activeLogs)
        {
            if (log != null) Destroy(log);
        }
        activeLogs.Clear();

        Transform container = contentContainer != null ? contentContainer : logTextBox.transform.parent;
        if (container == null) container = this.transform;

        // 0번 인덱스부터 현재 출력 중인 인덱스(maxIndex)까지만 로그 생성
        for (int i = 0; i <= maxIndex; i++)
        {
            string speaker = repo.GetRecordSpeakerByIndex(charTID, reqLevel, i);
            string body = repo.GetRecordTextByIndex(charTID, reqLevel, i);

            // 관제사가 아닌 대사이거나 관제사 대사라도 빈 문자열이 아니면 출력 (상황에 맞게 필터링 가능)
            if (!string.IsNullOrEmpty(body))
            {
                GameObject logObj = Instantiate(logTextBox, container);
                logObj.SetActive(true);
                
                LogTextBox boxScript = logObj.GetComponent<LogTextBox>();
                if (boxScript != null)
                {
                    boxScript.SetTextBox(speaker, body);
                }
                activeLogs.Add(logObj);
            }
        }
    }
}
