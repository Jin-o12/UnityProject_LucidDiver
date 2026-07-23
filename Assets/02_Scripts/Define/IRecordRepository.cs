using System.Collections.Generic;

/// <summary>
/// 기록(Record) 대사 가져오기 인터페이스
/// </summary>
public interface IRecordRepository
{
    /* 특정 캐릭터의 모든 기록(레코드 그룹) 해금 레벨 리스트를 반환하는 함수 */
    List<int> GetRecordGroupLevels(int charTID);

    /* 특정 레벨에 맞는 기록 대사 중 원하는 순서의 대사 텍스트를 뽑아오는 함수 */
    string GetRecordTextByIndex(int charTID, int requiredLevel, int index);

    /* 특정 레벨에 맞는 기록 대사 중 원하는 순서의 대사 발화자를 뽑아오는 함수 */
    string GetRecordSpeakerByIndex(int charTID, int requiredLevel, int index);

    /* 특정 레벨에서 출력 가능한 기록 대사의 개수를 뽑아오는 함수 */
    int GetRecordCount(int charTID, int requiredLevel);

    /* 특정 레벨에 맞는 기록의 제목(RecordName)을 뽑아오는 함수 */
    string GetRecordName(int charTID, int requiredLevel);

    /* 특정 레벨에 맞는 기록의 메인 이미지 주소를 뽑아오는 함수 */
    string GetRecordMainImage(int charTID, int requiredLevel);

    /* 특정 레벨에 맞는 기록의 배경 이미지 주소를 뽑아오는 함수 */
    string GetRecordBackgroundImage(int charTID, int requiredLevel);
}
