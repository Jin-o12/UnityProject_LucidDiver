/// <summary>
/// 캐릭터 데이터 가져오기 인터페이스
/// </summary>

public interface ICharDataRepository
{
    // 캐릭터 데이터 찾기
    CharacterData GetCharacterData(int _TID);
}
