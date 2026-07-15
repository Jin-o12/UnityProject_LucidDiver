using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 개별 대사 한 줄
[System.Serializable]
public class DialogueLine
{
    public int ID;
    public string Text;
    public int RequiredLevel;
}

[System.Serializable]
public class CharacterDialogueData
{
    public int CharacterTID;
    public string CharacterName;

    public Dictionary<DialogueType, List<DialogueLine>> Dialogues;
}
