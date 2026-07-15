using AnyPortrait;
using System.Collections;
/// <summary>
/// 플레이어의 이동과 커서 방향 회전을 처리하는 스크립트
/// </summary>
using UnityEngine;
using static AnyPortrait.apAnimPlayUnit;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement Controll")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera mainCamera;

    private Vector2 movementInput;                      // 플레이어의 이동 입력
    private bool sprintInput;                           // 플레이어의 달리기 입력
    private Vector2 currentMousePos;                    // 현재 마우스 화면 좌표

    private readonly float isometricYAngle = +45.0f;    // 쿼터뷰 기준 이동 방향 보정
    public float moveSpeed;                             // 이동 속도
    public float rotationSpeed = 1000f;                 // 회전 속도
    private float artifactMoveSpeedRate;                // 아티팩트로 추가되는 이동 속도 증가율

    public float sprintSpeed;                           // 달리기 중 이동 속도
    public float sprintMP;                              // 달리기 중 초당 MP 소비

    public bool isEvading;                              // 구르기 상태 체크
    public float evadeSpeed;                            // 구르기 이동 속도
    public float evadeTime;                             // 구르기 동작 시간
    public float evadeMP;                               // 구르기 MP 소비
    public float evadeCooltime;                         // 구르기 쿨타임

    [Header("Collision Guard")]
    [SerializeField] private LayerMask movementObstacleMask; // 이동/구르기 중 관통을 막을 벽 레이어
    [SerializeField] private float movementWallBuffer = 0.05f; // 벽 앞에서 멈추도록 남기는 여유 거리
    [SerializeField] private float evadeCornerProbeRadius = 0.25f; // 벽 모서리 관통을 막기 위한 보조 검사 반지름
    [SerializeField] private float evadeCornerProbeHeight = 0.5f; // 보조 검사를 시작할 플레이어 높이

    [Header("Movement Audio")]
    public int[] FootStep_AudioIDPool = null;           // 이동 사운드 ID 리스트
    public int[] InDoor_FootStep_AudioIDPool = null;    // 실내 이동 사운드 ID 리스트 (실외/실내 구분 가능한 경우 실내에서 사용)
    public int[] Evade_AudioIDPool = null;              // 구르기 사운드 ID 리스트
    public int[] Throw_AudioIDPool = null;              // 스킬 동작 사운드 ID 리스트

    private float lastFootstepTime;                     // 최근 이동 사운드 출력 시점
    public float FootStep_SoundTime = 10.0f;            // 이동 사운드 ID 출력 시간 기준 값

    [Header("Noise Settings")]
    // 발소리는 "플레이어 이동 입력"이 아니라 실제 이동 중일 때 일정 간격으로만 발생시킵니다.
    [SerializeField] private float walkNoiseRange = 15.0f;
    [SerializeField] private float runNoiseRange = 20.0f;
    [SerializeField] private float walkNoiseInterval = 0.55f;
    [SerializeField] private float runNoiseInterval = 0.3f;

    [Header("Player Animation Controll")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject Body;

    [SerializeField] public apPortrait apPort;              // AnyPortrait 캐릭터 애니메이션 컨트롤러

    private float moveNoiseTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 이동에 반드시 필요한 컴포넌트가 없으면 비활성화
        // Additive 씬 로드 타이밍상 Camera.main은 플레이어보다 늦게 잡힐 수 있으므로 카메라 누락만으로 이동을 막지 않습니다.
        if (rb == null || animator == null)
        {
            enabled = false;
            Debug.LogError("PlayerMovement: 필요한 컴포넌트가 없습니다.");
            return;
        }

        if (apPort != null)
        {
            apPort.enabled = true;
            if (apPort.gameObject != null) apPort.gameObject.SetActive(true);
        }

        lastFootstepTime = Time.time;
    }

    private void OnEnable()
    {
        GlobalEventBus.OnPlayerMove += PlayerMove;
        GlobalEventBus.SendCanSprint += PlayerSprint;
        GlobalEventBus.SendCannotSprint += PlayerSprint;
        GlobalEventBus.OnMousePositionInput += UpdateMousePos;
        GlobalEventBus.OnMainActiveSkillCasted += SkillAnimate;
        GlobalEventBus.OnHitAnimate += HitAnimate;
        GlobalEventBus.onPlayerDead += PlayerDie;
    }

    private void OnDisable()
    {
        GlobalEventBus.OnPlayerMove -= PlayerMove;
        GlobalEventBus.SendCanSprint -= PlayerSprint;
        GlobalEventBus.SendCannotSprint -= PlayerSprint;
        GlobalEventBus.OnMousePositionInput -= UpdateMousePos;
        GlobalEventBus.OnMainActiveSkillCasted -= SkillAnimate;
        GlobalEventBus.OnHitAnimate -= HitAnimate;
        GlobalEventBus.onPlayerDead -= PlayerDie;
        moveNoiseTimer = 0.0f;

        // 비활성화 시 입력 상태를 초기화하여
        // 재활성화 시 이전 입력 값이 남아 걷기 모션이 유지되는 것을 방지
        movementInput = Vector2.zero;
        sprintInput = false;
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsSprint", false);
        }
    }

    private void FixedUpdate()
    {
        // 메인 카메라 연결이 끊겼다면 갱신
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main;
        }

        MoveAndRotate();
    }

    public void initialize(CharacterData _charData)
    {
        // 기본 이동 속도 초기화
        moveSpeed = _charData.moveSpeed;

        // 달리기 속도 초기화
        sprintSpeed = _charData.sprintSpeed;

        // 달리기 중 초당 MP 소비 초기화
        sprintMP = _charData.sprintMana;

        // 구르기 이동 거리(속도 × 시간) 초기화
        evadeSpeed = _charData.evadeSpeed;
        evadeTime = _charData.evadeTime;

        // 구르기 MP 소비 초기화
        evadeMP = _charData.evadeMana;

        // 구르기 쿨타임 초기화
        evadeCooltime = _charData.evadeCooltime;

        artifactMoveSpeedRate = 0.0f;
        movementWallBuffer = Mathf.Max(0.0f, movementWallBuffer);
        evadeCornerProbeRadius = Mathf.Max(0.05f, evadeCornerProbeRadius);
        evadeCornerProbeHeight = Mathf.Max(0.0f, evadeCornerProbeHeight);
    }

    /// <summary>
    /// 아티팩트 장착 효과로 추가되는 이동 속도 증가율을 갱신합니다.
    /// 원본 moveSpeed/sprintSpeed를 직접 바꾸지 않고, 실제 이동 계산에서만 배율로 적용합니다.
    /// </summary>
    public void ApplyArtifactMoveSpeedBonus(float moveSpeedRate)
    {
        artifactMoveSpeedRate = Mathf.Max(0.0f, moveSpeedRate);
    }

    /* 플레이어 이동 입력 갱신 */
    private void PlayerMove(Vector2 input) => movementInput = input;

    /* 플레이어 달리기 입력 갱신 */
    private void PlayerSprint(bool sprint) => sprintInput = sprint;

    /* 마우스 화면 좌표 갱신 */
    private void UpdateMousePos(Vector2 pos) => currentMousePos = pos;

    /* 이동과 회전 처리 */
    private void MoveAndRotate()
    {
        Vector3 inputDir = new Vector3(movementInput.x, 0f, movementInput.y);
        Quaternion isoRotation = Quaternion.Euler(0f, isometricYAngle, 0f);
        Vector3 movement = (isoRotation * inputDir).normalized;

        float moveSpeedMultiplier = 1.0f + artifactMoveSpeedRate;
        float currentMoveSpeed = moveSpeed * moveSpeedMultiplier;
        float currentSprintSpeed = sprintSpeed * moveSpeedMultiplier;
        float finalMoveSpeed = isEvading ? evadeSpeed : (sprintInput ? currentSprintSpeed : currentMoveSpeed);
        Vector3 targetPosition = rb.position + movement * finalMoveSpeed * Time.fixedDeltaTime;
        if (isEvading)
        {
            targetPosition = GetSafeMovePosition(targetPosition);
        }

        // 달리기 중 MP 소비 이벤트 전달
        if (movement.sqrMagnitude > 0.001f && sprintInput)
        {
            GlobalEventBus.OnSprintManaConsume?.Invoke(sprintMP);
            GlobalEventBus.OnSprintInput?.Invoke(sprintInput);
        }

        rb.MovePosition(targetPosition);
        ClearHorizontalVelocity();
        EmitMovementNoise(movement.sqrMagnitude > 0.001f);

        // 이동 방향에 따라 바라보는 방향 회전
        if (movementInput.x > 0)
        {
            Body.transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (movementInput.x < 0)
        {
            Body.transform.localScale = new Vector3(1, 1, 1);
        }

        // 위치 이동 중 정해진 시간 간격마다 이동 사운드를 재생
        if (movement.sqrMagnitude >= 0.1f) FootStepAudio();

        AimTowardsMouse();
        ImageTowardsMouse();
    }

    private void FootStepAudio()
    {
        float walkFootStepTime = FootStep_SoundTime / moveSpeed;      // 걷기 사운드 재생 간격(초)
        float sprintFootStepTime = FootStep_SoundTime / sprintSpeed;  // 달리기 사운드 재생 간격(초)

        // 달리기 중인지에 따라 사운드 재생 간격 선택
        float footstepPeriod = sprintInput ? sprintFootStepTime : walkFootStepTime;

        // 최근 사운드 재생 시점으로부터 재생 간격만큼 지났는지 확인
        if (Time.time < lastFootstepTime + footstepPeriod) return;

        // 사운드 재생 이벤트를 AudioManager에 전달하여 구르기를 실행한 지점에서 3D 오디오 재생
        int moveAudioID = FootStep_AudioIDPool[Random.Range(0, FootStep_AudioIDPool.Length)];
        GlobalEventBus.OnPlay3DSoundRequested?.Invoke(moveAudioID, rb.gameObject.transform.position);

        // 최근 사운드 재생 시점 갱신
        lastFootstepTime = Time.time;
    }

    /// <summary>
    /// MovePosition 이후 Rigidbody에 남은 수평 속도를 정리합니다.
    /// 벽에 비비며 구른 뒤 입력을 떼었을 때 충돌 분리 속도 때문에 미끄러지는 현상을 줄입니다.
    /// </summary>
    private void ClearHorizontalVelocity()
    {
        Vector3 velocity = rb.velocity;
        velocity.x = 0.0f;
        velocity.z = 0.0f;
        rb.velocity = velocity;
        rb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// 구르기 이동 전에 Rigidbody가 실제로 지나갈 경로를 검사해 벽을 관통하지 않도록 목표 위치를 보정합니다.
    /// 일반 이동까지 막으면 문/벽 근처 상호작용 거리 진입이 어려워질 수 있으므로 회피 중에만 사용합니다.
    /// </summary>
    private Vector3 GetSafeMovePosition(Vector3 targetPosition)
    {
        Vector3 moveDelta = targetPosition - rb.position;
        moveDelta.y = 0.0f;

        if (moveDelta.sqrMagnitude <= 0.0001f)
        {
            return targetPosition;
        }

        LayerMask obstacleMask = ResolveMovementObstacleMask();
        if (obstacleMask.value == 0)
        {
            return targetPosition;
        }

        Vector3 direction = moveDelta.normalized;
        float distance = moveDelta.magnitude;

        if (rb.SweepTest(direction, out RaycastHit hit, distance, QueryTriggerInteraction.Ignore) &&
            ((1 << hit.collider.gameObject.layer) & obstacleMask.value) != 0)
        {
            float safeDistance = Mathf.Max(0.0f, hit.distance - movementWallBuffer);
            Vector3 safePosition = rb.position + direction * safeDistance;
            safePosition.y = rb.position.y;
            return safePosition;
        }

        if (Physics.SphereCast(
                rb.position + Vector3.up * evadeCornerProbeHeight,
                evadeCornerProbeRadius,
                direction,
                out RaycastHit cornerHit,
                distance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0.0f, cornerHit.distance - movementWallBuffer);
            Vector3 safePosition = rb.position + direction * safeDistance;
            safePosition.y = rb.position.y;
            return safePosition;
        }

        return targetPosition;
    }

    /// <summary>
    /// 인스펙터에서 따로 지정하지 않았으면 프로젝트의 Wall 레이어를 기본 이동 차단 레이어로 사용합니다.
    /// </summary>
    private LayerMask ResolveMovementObstacleMask()
    {
        if (movementObstacleMask.value != 0)
        {
            return movementObstacleMask;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        return wallLayer >= 0 ? 1 << wallLayer : 0;
    }

    private void EmitMovementNoise(bool isMoving)
    {
        if (!isMoving || isEvading)
        {
            moveNoiseTimer = 0.0f;
            return;
        }

        // 매 프레임 소음을 만들지 않고, 걷기/달리기 상태에 따라 간격을 두고 보냅니다.
        moveNoiseTimer -= Time.fixedDeltaTime;
        if (moveNoiseTimer > 0.0f)
        {
            return;
        }

        if (sprintInput)
        {
            NoiseSystem.Emit(NoiseType.Run, transform.position, gameObject, runNoiseRange);
            VFXService.Instance?.Play(GameplayVFXIds.PlayerFootstepRun, transform.position, transform.rotation);
            moveNoiseTimer = Mathf.Max(0.05f, runNoiseInterval);
            return;
        }

        NoiseSystem.Emit(NoiseType.Walk, transform.position, gameObject, walkNoiseRange);
        VFXService.Instance?.Play(GameplayVFXIds.PlayerFootstepWalk, transform.position, transform.rotation);
        moveNoiseTimer = Mathf.Max(0.05f, walkNoiseInterval);
    }

    /* 구르기 처리 */
    public void PlayerEvade()
    {
        isEvading = true;

        VFXService.Instance?.Play(GameplayVFXIds.PlayerEvade, transform.position, transform.rotation);

        // 구르기 입력을 애니메이터에 전달
        animator.SetTrigger("Evade");

        // 사운드 재생 이벤트를 AudioManager에 전달하여 구르기를 실행한 지점에서 3D 오디오 재생
        int EvadeAudioID = Evade_AudioIDPool[Random.Range(0, Evade_AudioIDPool.Length)];
        GlobalEventBus.OnPlay3DSoundRequested?.Invoke(EvadeAudioID, rb.gameObject.transform.position);

        // 구르기 상태 종료는 코루틴으로 처리
        StartCoroutine(EvadeComplete());
    }

    /* 구르기 상태 종료 코루틴 */
    private IEnumerator EvadeComplete()
    {
        yield return new WaitForSeconds(evadeTime);
        isEvading = false;
    }

    /* 스킬 사용 애니메이션 트리거 */
    public void SkillAnimate()
    {
        animator.SetTrigger("UseSkill");

        // 사운드 재생 이벤트를 AudioManager에 전달하여 스킬을 사용한 지점에서 3D 오디오 재생
        int SkillAudioID = Throw_AudioIDPool[Random.Range(0, Throw_AudioIDPool.Length)];
        GlobalEventBus.OnPlay3DSoundRequested?.Invoke(SkillAudioID, rb.gameObject.transform.position);
    }

    /* 피격 애니메이션 코루틴 */
    public void HitAnimate() => StartCoroutine(HitFaceAnimation());
    public IEnumerator HitFaceAnimation()
    {
        if (apPort != null)
        {
            // 히트 시 이벤트를 받아서 Hit_Face 변수를 1로 만듦
            apPort.SetControlParamFloat("Yuan_Hit_Face", 1);
            yield return new WaitForSeconds(0.75f);
            apPort.SetControlParamFloat("Yuan_Hit_Face", 0);
        }
    }

    /* 마우스 커서가 가리키는 월드 위치 계산 */
    private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            worldPoint = Vector3.zero;
            return false;
        }

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

        // 달리기 입력이 있으면 true, 없으면 false
        animator.SetBool("IsSprint", sprintInput);
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
