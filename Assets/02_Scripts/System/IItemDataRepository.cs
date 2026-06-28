/// <summary>
/// 아이템 데이터 가져오기 인터페이스
/// </summary>

public interface IItemDataRepository
{
    // 아이템 데이터 가져오기
    public ItemData GetItemData(int itemTID);
}
