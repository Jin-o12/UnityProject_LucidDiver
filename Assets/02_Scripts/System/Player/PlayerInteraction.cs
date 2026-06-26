using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 주변의 상호작용 가능한 오브젝트를 감지하고
/// 입력이 들어왔을 때 가장 적절한 대상과 상호작용을 실행하는 스크립트
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();    // 현재 상호작용 범위 안에 들어와 있는 대상 목록
    private EntityIdentity identity;                                                // 플레이어의 ID 정보 참조

    private void Awake()
    {
        identity = GetComponentInParent<EntityIdentity>();

        // 플레이어의 식별 정보가 없으면 상호작용 처리 불가능
        if (identity == null)
        {
            enabled = false;
            Debug.LogError("PlayerInteraction: 필요한 컴포넌트가 없습니다.");
        }
    }

    private void OnEnable()
    {
        /// 이벤트 구독 ///
        GlobalEventBus.OnInteractionInput += GetInteraction;
    }

    private void OnDisable()
    {
        /// 이벤트 구독 해제 ///
        GlobalEventBus.OnInteractionInput -= GetInteraction;
    }

    /// <summary>
    /// 상호작용 감지 범위 안으로 오브젝트가 들어오면 목록에 등록하고
    /// 해당 오브젝트의 상호작용 프롬프트를 켬
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 콜라이더 자신이 아니라 부모 쪽에서 IInteractable을 찾음
        // 자식 콜라이더를 쓰는 구조에서도 안정적으로 찾기 위함
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null || nearbyInteractables.Contains(interactable))
            return;

        nearbyInteractables.Add(interactable);
        SetPromptVisible(interactable, true);

        Debug.Log("Interactable detected: " + other.gameObject.name);
    }

    /// <summary>
    /// 상호작용 감지 범위 밖으로 오브젝트가 나가면 목록에서 제거하고
    /// 해당 오브젝트의 상호작용 프롬프트를 끔
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null)
            return;

        if (nearbyInteractables.Remove(interactable))
        {
            SetPromptVisible(interactable, false);
        }
    }

    /// <summary>
    /// 상호작용 입력이 들어오면 현재 범위 안 대상 중
    /// 가장 가까운 오브젝트를 선택해서 상호작용을 실행함
    /// </summary>
    private void GetInteraction()
    {
        // 이미 파괴된 오브젝트가 리스트에 남아 있을 수 있으므로 먼저 정리
        CleanupInvalidInteractables();

        Debug.Log("인터렉션 입력이 실행됨. 근처 상호작용 가능 요소 수: " + nearbyInteractables.Count);

        if (nearbyInteractables.Count == 0)
            return;

        IInteractable target = GetClosestInteractable();
        if (target == null)
            return;

        bool shouldRemove = target.Interact(identity.entityID);

        // 상호작용 후 제거되는 대상이면 프롬프트를 끄고 목록에서도 제거
        if (shouldRemove)
        {
            SetPromptVisible(target, false);
            nearbyInteractables.Remove(target);
        }
    }

    /// <summary>
    /// 현재 범위 안의 상호작용 대상 중 플레이어와 가장 가까운 대상을 반환
    /// sqrMagnitude를 사용해 불필요한 루트 계산을 줄임
    /// </summary>
    private IInteractable GetClosestInteractable()
    {
        IInteractable closest = null;
        float closestSqrDistance = float.MaxValue;
        Vector3 playerPosition = identity.transform.position;

        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            Component interactableComponent = nearbyInteractables[i] as Component;
            if (interactableComponent == null)
                continue;

            float sqrDistance = (interactableComponent.transform.position - playerPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = nearbyInteractables[i];
            }
        }

        return closest;
    }

    /// <summary>
    /// 상호작용 오브젝트에 연결된 프롬프트 UI를 켜거나 끔
    /// ItemBox 루트에 InteractionPromptUI가 붙어 있다는 전제
    /// </summary>
    private void SetPromptVisible(IInteractable interactable, bool isVisible)
    {
        Component interactableComponent = interactable as Component;
        if (interactableComponent == null)
            return;

        InteractionPromptUI promptUI = interactableComponent.GetComponent<InteractionPromptUI>();
        if (promptUI == null)
            return;

        if (isVisible)
            promptUI.Show();
        else
            promptUI.Hide();
    }

    /// <summary>
    /// 이미 파괴되었거나 유효하지 않은 상호작용 대상을 리스트에서 제거
    /// </summary>
    private void CleanupInvalidInteractables()
    {
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            Component interactableComponent = nearbyInteractables[i] as Component;
            if (interactableComponent == null)
            {
                nearbyInteractables.RemoveAt(i);
            }
        }
    }
}