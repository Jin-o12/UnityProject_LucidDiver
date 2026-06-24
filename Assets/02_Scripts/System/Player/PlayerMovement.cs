/// <summary>
/// 플레이어의 이동을 처리하는 스크립트
/// </summary>
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement Controll")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera mainCamera;
    private Vector2 movementInput;                      // 플레이어의 움직임 입력
    private readonly float isometricYAngle = -45.0f;    // 카메라가 보는 각도에 맞춰 플레이어 이동 방향 보정 (45도)
    public float moveSpeed;                             // 움직임 속도
    public float rotationSpeed = 10f;                   // 회전 속도

    [Header("Player Attack Movement Controll")]
    private Vector2 currentMousePos;                    // 현재 2차원상의 마우스 위치
    private float attackFocusTimer = 0f;                // 공격에 집중하는 시간 타이머
    private readonly float attackFocusDuration = 0.5f;  // 공격에 집중하는 시간 딜레이 (마우스를 바라보는 시간)

    [Header("Player Animation Controll")]
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // 필수 컴포넌트가 존재하지 않을 시 스크립트 비활성화
        if(rb==null || animator==null)
        {
            this.enabled = false;
            Debug.LogError("PlayerMovement: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnPlayerMove += PlayerMove;
        GlobalEventBus.OnAttackInput += OnAttack;
        GlobalEventBus.OnMousePositionInput += UpdateMousePos;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnPlayerMove -= PlayerMove;
        GlobalEventBus.OnAttackInput -= OnAttack;
        GlobalEventBus.OnMousePositionInput -= UpdateMousePos;
    }

    public void initialize(float _speed)
    {
        moveSpeed = _speed;
    }

    /* 플레이어 이동 값을 받아 전달 */
    private void PlayerMove(Vector2 input) => movementInput = input;
    /* 마우스 위치값을 받아 전달 */
    private void UpdateMousePos(Vector2 pos) => currentMousePos = pos;
    /* 공격 시 타이머를 가동시켜 회전 우선순위 전환 */
    private void OnAttack()
    {
        attackFocusTimer = attackFocusDuration;
    }
    
    private void FixedUpdate()
    {
        GetMove();
    }

    /* 플레이어 이동 및 회전 처리 */
    private void GetMove()
    {
        /// 이동 처리 ///
        // input system의 입력 값으로 평면상의 기본 벡터 생성
        Vector3 inputDir = new Vector3(movementInput.x, 0f, movementInput.y);
        // Y축을 기준으로 일정 각도 회전하는 수학적 행렬(Quaternion) 생성
        Quaternion isoRotation = Quaternion.Euler(0f, isometricYAngle, 0f);
        // 기본 입력 벡터를 일정 각도 회전시켜 쿼터뷰 화면에 맞게 보정
        // 주의: 반드시 Quaternion이 앞에 와야 합니다.
        Vector3 movement = isoRotation * inputDir;
        // 벡터 길이 정규화
        movement = movement.normalized;

        // 물리 엔진을 이용해 최종 이동
        Vector3 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);

        /// 회전 처리 ///
        // 공격 시의 회전을 우선 순위로 처리
        if(attackFocusTimer > 0)
        {
            attackFocusTimer -= Time.fixedDeltaTime;
            AimTowardsMouse();
        }
        // 이후 이동 시 회전에 대한 로직 처리
        else if(movement != Vector3.zero)
        {
            // 이동하고자 하는 방향을 바라보는 목표 회전 각도를 계산
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            // 목표 각도까지 부드럽게 보간(Slerp)
            Quaternion smoothRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            // 물리 엔진을 이용해 최종적으로 회전을 적용
            rb.MoveRotation(smoothRotation);
        }
    }

    private void AimTowardsMouse()
    {
        Plane playerPlane = new Plane(Vector3.up, transform.position);
        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);

        if(playerPlane.Raycast(ray, out float hit))
        {
            Vector3 hitPosition = ray.GetPoint(hit);
            Vector3 aimDirection = hitPosition - transform.position;
            aimDirection.y = 0f;

            if (aimDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
            }
        }
    }
}
