using UnityEngine;

/// <summary>
/// 플레이어가 콜라이더에 진입하면 지정한 튜토리얼 ID의 설명 팝업을 요청합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private string triggerValue;
    [SerializeField] private string fallbackTutorialId = "";
    [SerializeField] private bool triggerOnce = true;

    [Header("전투 구간 차단 옵션")]
    [SerializeField] private bool useCombatBlockers = false;
    [SerializeField] private string combatBlockerNamePrefix = "CombatCollider";
    [SerializeField] private bool disableCombatBlockersOnAwake = true;
    [SerializeField] private bool releaseCombatBlockersOnEnemyDead = true;

    private bool consumed;
    private bool combatBlockersActive;
    private bool combatBlockersCleared;
    private Collider[] combatBlockers;

    public string TriggerValue => string.IsNullOrWhiteSpace(triggerValue) ? gameObject.name : triggerValue;
    public bool IsConsumed => consumed;

    private void Awake()
    {
        CacheCombatBlockers();

        // 전투 구간 차단 콜라이더는 플레이어가 해당 트리거에 진입하기 전까지 꺼 둡니다.
        if (useCombatBlockers && disableCombatBlockersOnAwake)
            SetCombatBlockers(false);
    }

    private void OnEnable()
    {
        if (useCombatBlockers && releaseCombatBlockersOnEnemyDead)
            GlobalEventBus.OnEnemyDead += HandleEnemyDead;
    }

    private void OnDisable()
    {
        if (useCombatBlockers && releaseCombatBlockersOnEnemyDead)
            GlobalEventBus.OnEnemyDead -= HandleEnemyDead;
    }

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(4f, 2f, 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        // 전투 실습 차단은 안내 팝업 성공 여부와 분리해서, 트리거에 닿는 즉시 먼저 켭니다.
        if (useCombatBlockers && !combatBlockersActive && !combatBlockersCleared)
            ActivateCombatBlockers();

        if (triggerOnce && consumed)
            return;

        TutorialManager manager = TutorialManager.Instance ?? FindFirstObjectByType<TutorialManager>();
        if (manager == null)
            return;

        bool handledByCondition = manager.NotifyTriggerEnter(TriggerValue);
        bool handledByFallback = !handledByCondition && !string.IsNullOrWhiteSpace(fallbackTutorialId) && manager.Show(fallbackTutorialId);

        if (handledByCondition || handledByFallback)
        {
            consumed = triggerOnce;
        }
    }

    public void ResetTrigger()
    {
        consumed = false;
        combatBlockersActive = false;
        combatBlockersCleared = false;

        if (useCombatBlockers)
            SetCombatBlockers(false);
    }

    private void ActivateCombatBlockers()
    {
        if (!useCombatBlockers)
            return;

        SetCombatBlockers(true);
        combatBlockersActive = true;
    }

    private void HandleEnemyDead(int enemyId)
    {
        if (!useCombatBlockers || !combatBlockersActive)
            return;

        // 전투 실습 구간에서는 적 처치 이벤트를 클리어 조건으로 보고 차단을 해제합니다.
        SetCombatBlockers(false);
        combatBlockersActive = false;
        combatBlockersCleared = true;
    }

    private void CacheCombatBlockers()
    {
        if (!useCombatBlockers)
            return;

        Collider[] childColliders = GetComponentsInChildren<Collider>(true);
        combatBlockers = System.Array.FindAll(childColliders, IsCombatBlocker);
    }

    private bool IsCombatBlocker(Collider target)
    {
        return target != null &&
               target.gameObject != gameObject &&
               target.gameObject.name.StartsWith(combatBlockerNamePrefix, System.StringComparison.Ordinal);
    }

    private void SetCombatBlockers(bool enabled)
    {
        if (combatBlockers == null || combatBlockers.Length == 0)
            CacheCombatBlockers();

        if (combatBlockers == null)
            return;

        foreach (Collider blocker in combatBlockers)
        {
            if (blocker == null)
                continue;

            blocker.enabled = enabled;
        }
    }

    private static bool IsPlayer(Collider other)
    {
        return other != null &&
               (other.CompareTag("Player") ||
                other.GetComponentInParent<PlayerStatus>() != null ||
                other.GetComponentInParent<LocalInputReader>() != null);
    }

    private void OnDrawGizmos()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger == null)
            return;

        Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.75f);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(trigger.center, trigger.size);
        Gizmos.matrix = previousMatrix;
    }
}
