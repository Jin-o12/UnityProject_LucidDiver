using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

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

    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 객체가 IInteractable을 구현하는지 확인
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log("Interactable detected: " + other.gameObject.name);
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 충돌이 끝난 객체가 IInteractable을 구현하는지 확인
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            nearbyInteractables.Remove(interactable);
        }
    }

    /* 상호작용 시 */
    private void GetInteraction()
    {
        Debug.Log("Interaction input received. Nearby interactables count: " + nearbyInteractables.Count);
        // 상호작용 상대에 따른 처리
        if(nearbyInteractables.Count > 0)
        {
            // 현재는 우선 순위를 따지지 않고 리스트의 첫번째 요소 선택
            IInteractable target = nearbyInteractables[0];
            // 대상에게 상호작용 실행
            bool shouldRemove = target.Interact();
            
            // 상호작용 후 제거 될 대상(아이템 등)이라면 리스트에서 제거
            if (shouldRemove)
            {
                nearbyInteractables.Remove(target);
            }
        }
        
    }
}
