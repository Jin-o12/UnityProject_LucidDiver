/// <summary>
/// 로컬 입력을 읽어서 글로벌 이벤트 버스에 전달하는 역할을 하는 클래스
/// </summary>
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputReader : MonoBehaviour
{
    // 플레이어의 입력을 받아 글로벌 이벤트 변수에 전달하는 함수들

    /* 플레이어 이동 입력 처리 */
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        
        GlobalEventBus.OnPlayerMove?.Invoke(moveInput);
    }

    /* 플레이어 공격 입력 처리 */
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GlobalEventBus.OnAttackInput?.Invoke();
        }
    }
}
