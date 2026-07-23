using System.Collections.Generic;

[System.Serializable]
public class RecordLine
{
    public int DialogID;
    public string Speaker;
    public string Text;
}

[System.Serializable]
public class CharacterRecordData
{
    public int CharacterTID;
    public string CharacterName;

    // RequiredLevel(해금 레벨)을 키로 하여, 해당 레벨의 기록 제목을 저장
    public Dictionary<int, string> RecordNames;

    // RequiredLevel(해금 레벨)을 키로 하여, 해당 레벨의 메인 이미지를 저장
    public Dictionary<int, string> MainImages;

    // RequiredLevel(해금 레벨)을 키로 하여, 해당 레벨의 배경 이미지를 저장
    public Dictionary<int, string> BackgroundImages;

    // RequiredLevel(해금 레벨)을 키로 하여, 해당 레벨의 대사 리스트를 저장
    public Dictionary<int, List<RecordLine>> Records;
}
