using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// JSON 데이터를 그대로 받아낼 1차원 평면 클래스
[System.Serializable]
public class FlatAudioData
{
    public int AudioID;         //사운드 ID
    public string AudioClip;    //오디오 클립 파일 이름
    public AudioType AudioType; //사운드 타입
    public float Volume;        //음량 크기
    public bool Loop;           //반복 재생 여부
}

public class LocalJsonAudioRepository : IAudioRepository
{
    // 사운드 데이터를 TID를 키값으로 보관하는 딕셔너리
    private Dictionary<int, AudioData> audioDB = new Dictionary<int, AudioData>();

    public LocalJsonAudioRepository()
    {
        LoadAudioData();
    }

    // 사운드 데이터를 JSON에서 불러옴
    public void LoadAudioData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("JSON/Sound");
    }

    /* 정해진 audioID에 해당하는 사운드 데이터를 딕셔너리에서 추출 */
    public AudioData GetAudioData(int audioID)
    {
        if (audioDB.TryGetValue(audioID, out AudioData _data))
        {
            return _data;
        }

        Debug.LogError($"[AudioRepository] ID {audioID}에 해당하는 사운드가 없습니다.");
        return null;
    }
}
