/// <summary>
/// 플레이어 저장 데이터 관리 인터페이스
/// </summary>

public interface ISaveRepository
{
    // 플레이어 저장 데이터 불러오기
    PlayerSaveData LoadSaveData();
    
    // 게임 데이터 저장
    void SaveGameData();

    // 외부에서 전달 된 세이브 데이터 저장
    void SaveGameData(PlayerSaveData saveData);

    /// <summary>
    /// 캐릭터 데이터 관련 메소드
    /// </summary>
    
    // 현재 캐릭터 데이터 리턴
    SaveCharacterData GetNowCharacterData();

    // 현재 동조율 경험치 값 증가
    void AddlinkRatePoint(float _point);
    // 현재 동조율 경험치 값 리턴
    float GetlinkRatePoint();

    // 특정 캐릭터의 동조율 레벨 리턴
    int GetLinkRateLevel();
}
