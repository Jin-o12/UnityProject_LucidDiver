/// <summary>
/// 사운드 데이터 가져오기 인터페이스
/// </summary>
public interface IAudioRepository
{
    // 오디오 ID로 사운드 데이터 추출하기
    AudioData GetAudioData(int audioID);
}
