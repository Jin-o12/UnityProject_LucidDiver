/// <summary>
/// 적 체력 바 UI를 갱신하는 클래스입니다.
/// </summary>
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("체력 UI")]
    [SerializeField] private Image hpBar;
    [SerializeField] private TMP_Text hpText;

    private EnemyStatus ownerStatus; // 이 UI가 따라갈 실제 적 상태 컴포넌트

    private void Awake()
    {
        ownerStatus = GetComponentInParent<EnemyStatus>();

        if (ownerStatus == null)
        {
            enabled = false;
            Debug.LogError("EnemyHealthUI: 상위 오브젝트에서 EnemyStatus를 찾지 못했습니다.");
        }
    }

    private void OnEnable()
    {
        if (ownerStatus == null)
        {
            return;
        }

        // 수정 이유:
        // 기존에는 모든 적 UI가 GlobalEventBus.OnEnemyHealthChanged를 함께 구독해서,
        // 한 적이 맞아도 모든 적 HP 바가 같이 갱신되는 문제가 있었습니다.
        // 이제는 자기 부모 EnemyStatus가 보내는 로컬 이벤트만 구독해서 자기 체력만 갱신합니다.
        ownerStatus.OnLocalHealthChanged += UpdateHealthBar;
        UpdateHealthBar(ownerStatus.hpCurrent, ownerStatus.hpMax);
    }

    private void OnDisable()
    {
        if (ownerStatus == null)
        {
            return;
        }

        ownerStatus.OnLocalHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float currentHp, float maxHp)
    {
        if (maxHp <= 0.0f)
        {
            hpBar.fillAmount = 0.0f;
            hpText.text = "0/0";
            return;
        }

        hpBar.fillAmount = currentHp / maxHp;
        hpText.text = $"{currentHp}/{maxHp}";
    }
}
