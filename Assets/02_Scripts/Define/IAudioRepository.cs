/// <summary>
/// 사운드 데이터 가져오기 인터페이스
/// </summary>
public interface IAudioRepository
{
    // 오디오 ID로 사운드 데이터 추출하기
    AudioData GetAudioData(int audioID);

    // 오디오 클립 파일 이름으로 AudioID를 조회 (존재하면 true와 audioID 반환)
    bool TryGetAudioIDByClipName(string clipName, out int audioID);
}
