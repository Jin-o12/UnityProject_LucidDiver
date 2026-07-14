using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openParameterName = "IsOpen";
    [SerializeField] private bool canToggle = true;

    [Header("Interaction")]
    [SerializeField] private GameObject interactionGuideUI;

    private bool isPlayerInRange;
    private bool isOpen;

    private void Awake()
    {
        // Animator가 수동 연결되지 않았을 때 자식에서 자동으로 찾는다
        if (doorAnimator == null)
            doorAnimator = GetComponentInChildren<Animator>();

        // 상호작용 안내 UI는 시작 시 숨긴다
        if (interactionGuideUI != null)
            interactionGuideUI.SetActive(false);
    }

    private void Update()
    {
        // 플레이어가 상호작용 범위 안에 없으면 입력을 무시한다
        if (!isPlayerInRange)
            return;

        // F 키 입력 시 문 상호작용을 실행한다
        if (Input.GetKeyDown(KeyCode.F))
            Interact();
    }

    private void Interact()
    {
        // 토글이 불가능한 문은 한 번 열린 뒤 다시 동작하지 않게 한다
        if (!canToggle && isOpen)
            return;

        // 문 열림 상태를 반전한다
        isOpen = !isOpen;

        string doorVfxId = isOpen ? GameplayVFXIds.DoorOpen : GameplayVFXIds.DoorClose;
        VFXService.Instance?.Play(doorVfxId, transform.position, transform.rotation);

        // Animator 파라미터를 변경해 문 애니메이션을 재생한다
        if (doorAnimator != null)
            doorAnimator.SetBool(openParameterName, isOpen);

        // 문이 열린 뒤 안내 UI를 숨긴다
        if (interactionGuideUI != null)
            interactionGuideUI.SetActive(!isOpen);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 상호작용 범위에 들어왔는지 확인한다
        if (!other.CompareTag("Player"))
            return;

        isPlayerInRange = true;

        // 문이 닫혀 있거나 토글 가능한 경우 상호작용 안내 UI를 표시한다
        if (interactionGuideUI != null && (!isOpen || canToggle))
            interactionGuideUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 상호작용 범위에서 나갔는지 확인한다
        if (!other.CompareTag("Player"))
            return;

        isPlayerInRange = false;

        // {범위 밖으로 나가면 상호작용 안내 UI를 숨긴다}
        if (interactionGuideUI != null)
            interactionGuideUI.SetActive(false);
    }
}
