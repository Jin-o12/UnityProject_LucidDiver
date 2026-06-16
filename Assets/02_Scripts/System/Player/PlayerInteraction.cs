using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();        // 플레이어 근처의 상호작용 가능한 (IInteractable) 오브젝트 리스트

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
            // 상호작용 가능한 오브젝트일 경우 리스트에 추가
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
            // 상호작용 가능 객체가 영역을 벗어나면 리스트에서 제거
            nearbyInteractables.Remove(interactable);
        }
    }

    /* 무언가와 상호작용 시 */
    private void GetInteraction()
    {
        Debug.Log("인터렉션 입력이 실행됩니다. 근처의 상호작용 가능한 요소: " + nearbyInteractables.Count);
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
