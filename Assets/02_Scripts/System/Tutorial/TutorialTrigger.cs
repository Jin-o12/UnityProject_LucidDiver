using UnityEngine;

/// <summary>
/// 플레이어가 콜라이더에 진입하면 지정한 튜토리얼 ID의 설명 팝업을 요청합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private string tutorialId = "TUTORIAL_MOVE_001";
    [SerializeField] private bool triggerOnce = true;

    private bool consumed;

    public string TutorialId => tutorialId;
    public bool IsConsumed => consumed;

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(4f, 2f, 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((triggerOnce && consumed) || !IsPlayer(other))
            return;

        TutorialManager manager = TutorialManager.Instance ?? FindFirstObjectByType<TutorialManager>();
        if (manager != null && manager.Show(tutorialId))
            consumed = triggerOnce;
    }

    public void ResetTrigger()
    {
        consumed = false;
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
