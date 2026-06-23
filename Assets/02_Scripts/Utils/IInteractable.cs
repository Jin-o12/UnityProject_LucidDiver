/// <summary>
/// 인터렉션 가능한 대상에 대한 기능을 정의하는 인터페이스
/// </summary>

public interface IInteractable
{
    // 상호작용 시 실행
    bool Interact(int playerID);
}
