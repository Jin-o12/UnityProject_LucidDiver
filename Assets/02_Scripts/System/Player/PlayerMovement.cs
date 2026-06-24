/// <summary>
/// 플레이어의 이동과 커서 방향 회전을 처리하는 스크립트
/// </summary>
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement Controll")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera mainCamera;

    private Vector2 movementInput;                       // 플레이어의 이동 입력
    private Vector2 currentMousePos;                    // 현재 마우스 화면 좌표

    private readonly float isometricYAngle = -45.0f;    // 쿼터뷰 기준 이동 방향 보정
    public float moveSpeed;                             // 이동 속도
    public float rotationSpeed = 10f;                   // 회전 속도

    [Header("Player Animation Controll")]
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 필수 컴포넌트가 없으면 비활성화
        if (rb == null || animator == null || mainCamera == null)
        {
            enabled = false;
            Debug.LogError("PlayerMovement: 필요한 컴포넌트가 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        GlobalEventBus.OnPlayerMove += PlayerMove;
        GlobalEventBus.OnMousePositionInput += UpdateMousePos;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnPlayerMove -= PlayerMove;
        GlobalEventBus.OnMousePositionInput -= UpdateMousePos;
    }

    private void FixedUpdate()
    {
        MoveAndRotate();
    }

    public void initialize(float _speed)
    {
        moveSpeed = _speed;
    }

    /* 플레이어 이동 입력 갱신 */
    private void PlayerMove(Vector2 input) => movementInput = input;

    /* 마우스 화면 좌표 갱신 */
    private void UpdateMousePos(Vector2 pos) => currentMousePos = pos;

    /* 이동과 회전 처리 */
    private void MoveAndRotate()
    {
        Vector3 inputDir = new Vector3(movementInput.x, 0f, movementInput.y);
        Quaternion isoRotation = Quaternion.Euler(0f, isometricYAngle, 0f);
        Vector3 movement = (isoRotation * inputDir).normalized;

        Vector3 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);

        AimTowardsMouse();
    }

    /* 마우스 커서가 가리키는 월드 위치 계산 */
    private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        Plane plane = new Plane(Vector3.up, transform.position);
        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);

        if (plane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    /* 항상 마우스 커서 방향을 바라보도록 회전 */
    private void AimTowardsMouse()
    {
        if (!TryGetMouseWorldPoint(out Vector3 worldPoint))
            return;

        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        Quaternion smoothRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRot);
    }
}