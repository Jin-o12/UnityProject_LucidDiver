/// <summary>
/// 아이템 데이터 가져오기 인터페이스
/// </summary>

public interface IItemDataRepository
{
    // 아이템 데이터 가져오기
    void LoadGameData();
    
    // 특정 코드의 아이템 데이터 추출
    ItemData GetItemDataByID(int itemTID);

    // 특정 타입의 아이템 데이터 추출
    T GetTypeItemData<T>(int itemID) where T: ItemData;
}
