using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 개별 대사 한 줄
[System.Serializable]
public class DialogueLine
{
    public int ID;                  // 대사 고유 ID
    public string Text;             // 대사 텍스트
    public string LinkRateLevel;    // 해당 대사가 출력되는 최소 동조율 값
}

// 캐릭터 스크립트 데이터 객체 클래스
public class CharacterDialogueData
{
    public int CharacterTID;
    public string CharacterName;

    // 스크립트 묶음 딕셔너리
    public Dictionary<DialogueType, List<DialogueLine>> Dialogues;
}


