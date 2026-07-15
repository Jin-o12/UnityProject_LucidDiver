/// <summary>
/// 대사 가져오기 인터페이스
/// </summary>

public interface IDialogueRepository
{
    /* 특정 상황에 맞는 대사 중 원하는 순서의 대사를 뽑아오는 함수 */
    string GetDialogueByIndex(int _charTID, DialogueType _type, int _index);

    /* 특정 상황에 맞는 대사 중 원하는 순서의 대사에서 TID 값을 뽑아오는 함수 */
    int GetTIDByIndex(int _charTID, DialogueType _type, int _index);

    /* 특정 상황에서 출력 가능한 대사의 개수를 뽑아오는 함수 */
    public int GetDialogueCount(int _charTID, DialogueType _type);

    /* 특정 상황의 캐릭터 대사 데이터 랜덤하게 가져오기 */
    public string GetRandomDialogue(int _charTID, DialogueType _type, int currentLevel = 0);
}
