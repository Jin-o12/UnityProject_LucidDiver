using Newtonsoft.Json;
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
    private Dictionary<int, AudioData> audioDB = new();

    public LocalJsonAudioRepository()
    {
        LoadAudioData();
    }

    // 사운드 데이터를 JSON에서 불러옴
    public void LoadAudioData()
    {
        // 파일을 로드하고 null 체크
        TextAsset jsonAsset = Resources.Load<TextAsset>("JSON/Sound");
        if (jsonAsset == null)
        {
            Debug.LogError("[AudioRepository] 사운드 데이터 JSON 파일을 찾을 수 없습니다.");
            return;
        }

        // JSON 역직렬화하여 Flat 데이터 추출
        List<FlatAudioData> flatDataList = JsonConvert.DeserializeObject<List<FlatAudioData>>(jsonAsset.text);

        // 중복 방지를 위해 기존 DB를 초기화
        audioDB.Clear();

        // 추출한 Flat 데이터를 변환
        foreach (FlatAudioData data in flatDataList)
        {
            AudioData audio = ScriptableObject.CreateInstance<AudioData>();

            audio.AudioID = data.AudioID;
            audio.AudioClip = data.AudioClip;
            audio.AudioType = data.AudioType;
            audio.Volume = data.Volume;
            audio.Loop = data.Loop;

            // 변환한 데이터를 DB 딕셔너리에 등록
            audioDB[audio.AudioID] = audio;
        }
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

    // 오디오 클립 파일 이름으로 AudioID를 조회. 찾으면 true와 함께 audioID를 반환.
    public bool TryGetAudioIDByClipName(string clipName, out int audioID)
    {
        // 파일 이름을 입력하지 않았으면 false와 함께 "audioID = default 값"을 반환
        if (string.IsNullOrEmpty(clipName))
        {
            audioID = default;
            return false;
        }

        foreach (var kvp in audioDB)
        {
            // AudioClip 필드와 비교 (대소문자 무시)
            if (string.Equals(kvp.Value.AudioClip, clipName, System.StringComparison.OrdinalIgnoreCase))
            {
                audioID = kvp.Key;
                return true;
            }
        }

        // 못 찾았으면 false와 함께 "audioID = default 값"을 반환
        audioID = default;
        return false;
    }
}
