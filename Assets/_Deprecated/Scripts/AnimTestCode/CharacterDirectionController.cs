using UnityEngine;
using AnyPortrait;

public class CharacterDirectionController : MonoBehaviour
{
    public Animator animator;
    public apPortrait Yuan;

    void Start()
    {
    }

    void Update()
    {
        // W/S 키 또는 조이스틱의 상하 입력을 받습니다. (-1.0 ~ 1.0)
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        // 입력받은 값을 블렌드 트리의 LookY 파라미터로 전달합니다.
        // 캐릭터가 위로 가면(1) 뒷모습이, 아래로 가면(-1) 앞모습이 자연스럽게 재생됩니다.
        animator.SetFloat("SpeedY", verticalInput);
        animator.SetFloat("SpeedX", horizontalInput);
    }
}