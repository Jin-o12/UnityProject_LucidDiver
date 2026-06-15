/// <summary>
/// 글로벌 이벤트 버스 클래스
/// 게임 내에서 발생하는 다양한 이벤트를 중앙에서 관리하고 전달하는 역할을 합니다.
/// </summary>
using System;
using UnityEngine;

public class GlobalEventBus
{
    /// 플레이어 행동에 의한 이벤트 ///
    public static Action<Vector2> OnPlayerMove;
    public static Action OnAttackInput;
    public static Action OnInteractionInput;

    /// 아이템 관련 이벤트 ///
    public static Action<int> OnItemPickedUp;
    public static Action<int> OnWeaponEquipped;
}
