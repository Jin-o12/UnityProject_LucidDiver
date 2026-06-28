/// <summary>
/// 플레이어 저장 데이터 관리 인터페이스
/// </summary>

public interface ISaveRepository
{
    // 플레이어 저장 데이터 불러오기
    PlayerSaveData LoadSaveData();
    
    // 게임 데이터 저장
    void SaveGameData();

    // 특정 캐릭터의 동조율 레벨 리턴
    int GetLinkRateLevel();
}
