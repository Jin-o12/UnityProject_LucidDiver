/// <summary>
/// 플레이어의 이동과 커서 방향 회전을 처리하는 스크립트
/// </summary>
using UnityEngine;
using AnyPortrait;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement Controll")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera mainCamera;

    private Vector2 movementInput;                       // 플레이어의 이동 입력
    private Vector2 currentMousePos;                    // 현재 마우스 화면 좌표

    private readonly float isometricYAngle = -45.0f;    // 쿼터뷰 기준 이동 방향 보정
    public float moveSpeed;                             // 이동 속도
    public float rotationSpeed = 1000f;                   // 회전 속도

    [Header("Player Animation Controll")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject Body;

    [SerializeField] public apPortrait apPort;              // AnyPortrait 캐릭터 애니메이션 컨트롤러

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        GlobalEventBus.onPlayerDead += PlayerDie;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnPlayerMove -= PlayerMove;
        GlobalEventBus.OnMousePositionInput -= UpdateMousePos;
        GlobalEventBus.onPlayerDead -= PlayerDie;
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

        // 이동 방향에 따라 바라보는 방향 회전
        if(movementInput.x>0)
        {
            Body.transform.localScale = new Vector3(-1, 1, 1);
        }
        else if(movementInput.x<0)
        {
            Body.transform.localScale = new Vector3(1, 1, 1);
        }

        AimTowardsMouse();
        ImageTowardsMouse();
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

    /* 항상 마우스 커서 방향을 바라보도록 실제 몸체 회전 */
    private void AimTowardsMouse()
    {
        // 마우스가 월드를 가리키지 않으면 리턴
        if (!TryGetMouseWorldPoint(out Vector3 worldPoint)) return;

        // (마우스 방향 - 자신의 위치)로 마우스 방향 벡터를 구하고, y축은 0으로 고정하여 바닥 평면 기준으로 계산
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;

        // 마우스 방향 변화가 거의 없으면 리턴
        if (dir.sqrMagnitude < 0.001f) return;

        // 물리엔진을 이용해 캐릭터를 회전 (목표 회전 방향으로 보간된 현재 회전 방향 계산 및 적용)
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        Quaternion smoothRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRot);
    }
    
    /* 이미지가 마우스 커서 방향을 바라보도록 조정 */
    private void ImageTowardsMouse()
    {
        // 마우스가 월드를 가리키지 않으면 리턴
        if (!TryGetMouseWorldPoint(out Vector3 worldPoint)) return;

        // (마우스 방향 - 자신의 위치)로 마우스 방향 벡터를 구하고, y축은 0으로 고정하여 바닥 평면 기준으로 계산
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;

        // 마우스 방향을 2D 애니메이터용 방향(상하좌우)으로 변환
        // 쿼터뷰의 이동 계산을 위해 곱했던 isometricYAngle(-45도)의 반대 각도(45도)를 곱하여 화면 기준의 방향을 구함
        Quaternion inverseIsoRotation = Quaternion.Euler(0f, -isometricYAngle, 0f);
        Vector3 aimVisualDir = inverseIsoRotation * dir.normalized;

        // 마우스 방향(aimVisualDir.x)에 따른 시각적 좌우 반전
        if (aimVisualDir.x > 0)
        {
            Body.transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (aimVisualDir.x < 0)
        {
            Body.transform.localScale = new Vector3(1, 1, 1);
        }

        /// 애니 포트레이틑 파라미터 제어를 통한 세밀한 손의 회전(마우스를 향항 총구 조준) ///
        // 마우스 방향을 이용해 팔 회전 각도 계산
        if (apPort != null)
        {
            // 마우스가 캐릭터 기준 위쪽에 있을 때
            if(aimVisualDir.z > 0)
            {
                apPort.SetControlParamFloat("Yuan_B_AimY", -1 * Mathf.Lerp(-1f, 1f, aimVisualDir.z));
            }
            else
            {
                apPort.SetControlParamFloat("Yuan_AimY", Mathf.Lerp(-1f, 1f, Mathf.Abs(aimVisualDir.z)));
            }
        }

        /// 애니메이션 파라미터 ///
        // 키보드 입력이 있으면 true, 없으면 false
        bool isMoving = movementInput.sqrMagnitude > 0.01f;
        animator.SetBool("IsMoving", isMoving);

        int lookDir = 0; 
        // Z값이 양수면 위(뒷모습), 음수면 아래(앞모습)
        lookDir = aimVisualDir.z > 0 ? 1 : 0;

        animator.SetInteger("LookDir", lookDir);
    }

    /* 플레이어 사망 시 이동 및 회전 비활성화 */
    private void PlayerDie(int playerID)
    {
        enabled = false;
        rb.velocity = Vector3.zero;
        animator.SetBool("IsMoving", false);
        animator.SetInteger("LookDir", 0);
        animator.SetTrigger("IsDead");
    }
}